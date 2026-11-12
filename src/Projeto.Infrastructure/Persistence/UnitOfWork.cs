using Microsoft.EntityFrameworkCore;
using Projeto.Application.Abstractions;
using Projeto.Application.Common.Exceptions;

namespace Projeto.Infrastructure.Persistence;

internal sealed class UnitOfWork(AppDbContext context) : IUnitOfWork
{
    public async Task SalvarAlteracoesAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Um único SaveChanges = uma transação: INSERT do pedido + itens e UPDATE do estoque
            // (com checagem de RowVersion) são aplicados juntos ou nenhum é (RN10).
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConflitoConcorrenciaException(
                "O estoque de um dos produtos foi alterado por outro pedido ao mesmo tempo. Tente novamente.", ex);
        }
    }
}
