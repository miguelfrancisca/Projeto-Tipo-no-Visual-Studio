#!/usr/bin/env node
// Hook PostToolUse (Edit | Write | MultiEdit).
//
// Depois que o Claude edita um arquivo, valida que o código continua compilando:
//   - .cs / .csproj / .props  -> `dotnet build` do projeto .csproj mais próximo do arquivo
//   - .ts / .tsx              -> `tsc -b` no frontend
//
// Se a verificação falhar, sai com código 2 e escreve os erros no stderr:
// o Claude Code devolve essa saída ao modelo, que corrige o erro antes de seguir.
// Qualquer outro tipo de arquivo é ignorado (exit 0).

import { spawnSync } from "node:child_process";
import { existsSync, readdirSync, readFileSync } from "node:fs";
import path from "node:path";

const MAX_LINHAS_ERRO = 40;

function lerEntrada() {
  try {
    return JSON.parse(readFileSync(0, "utf8"));
  } catch {
    return null;
  }
}

function csprojMaisProximo(arquivo, raiz) {
  let dir = path.dirname(arquivo);
  while (dir.startsWith(raiz)) {
    const csproj = readdirSync(dir).find((f) => f.endsWith(".csproj"));
    if (csproj) return path.join(dir, csproj);
    const pai = path.dirname(dir);
    if (pai === dir) break;
    dir = pai;
  }
  return null;
}

function executar(comando, args, cwd) {
  const r = spawnSync(comando, args, { cwd, encoding: "utf8", shell: process.platform === "win32" });
  return { codigo: r.status ?? 1, saida: `${r.stdout ?? ""}\n${r.stderr ?? ""}` };
}

function falhar(titulo, saida, filtro) {
  const linhas = saida
    .split(/\r?\n/)
    .filter((l) => filtro.test(l))
    .map((l) => l.trim());
  const unicas = [...new Set(linhas)].slice(0, MAX_LINHAS_ERRO);
  process.stderr.write(
    `${titulo}\n${unicas.length ? unicas.join("\n") : saida.slice(-4000)}\n` +
      "Corrija esses erros antes de continuar.\n",
  );
  process.exit(2);
}

const entrada = lerEntrada();
const arquivo = entrada?.tool_input?.file_path;
if (!arquivo) process.exit(0);

const raiz = path.resolve(process.env.CLAUDE_PROJECT_DIR ?? entrada.cwd ?? process.cwd());
const absoluto = path.resolve(raiz, arquivo);
const ext = path.extname(absoluto).toLowerCase();

if ([".cs", ".csproj", ".props"].includes(ext)) {
  // Arquivos .props da raiz afetam a solução inteira; os demais, só o projeto do arquivo.
  const alvo = ext === ".props" ? raiz : csprojMaisProximo(absoluto, raiz);
  if (!alvo) process.exit(0);
  const { codigo, saida } = executar("dotnet", ["build", `"${alvo}"`, "-nologo", "-v", "q"], raiz);
  if (codigo !== 0) falhar(`dotnet build falhou após editar ${path.relative(raiz, absoluto)}:`, saida, /error [A-Z]+\d+/);
  process.exit(0);
}

if ([".ts", ".tsx"].includes(ext)) {
  const frontend = path.join(raiz, "frontend");
  if (!absoluto.startsWith(frontend) || !existsSync(path.join(frontend, "node_modules"))) process.exit(0);
  const { codigo, saida } = executar("npx", ["--no-install", "tsc", "-b"], frontend);
  if (codigo !== 0) falhar(`tsc falhou após editar ${path.relative(raiz, absoluto)}:`, saida, /error TS\d+/);
  process.exit(0);
}

process.exit(0);
