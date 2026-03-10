using Microsoft.EntityFrameworkCore;
using SGEEP.Core.Entities;
using SGEEP.Infrastructure.Data;

namespace SGEEP.Web.Services
{
    public class NotificacaoService
    {
        private readonly ApplicationDbContext _context;

        public NotificacaoService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task CriarAsync(string userId, string titulo, string mensagem)
        {
            _context.Notificacoes.Add(new Notificacao
            {
                ApplicationUserId = userId,
                Titulo = titulo,
                Mensagem = mensagem
            });
            await _context.SaveChangesAsync();
        }

        public async Task<int> ContarNaoLidasAsync(string userId)
        {
            return await _context.Notificacoes
                .CountAsync(n => n.ApplicationUserId == userId && !n.Lida);
        }

        public async Task<List<Notificacao>> ObterUltimasAsync(string userId, int quantidade = 20)
        {
            return await _context.Notificacoes
                .Where(n => n.ApplicationUserId == userId)
                .OrderByDescending(n => n.DataCriacao)
                .Take(quantidade)
                .ToListAsync();
        }

        public async Task MarcarComoLidaAsync(int id, string userId)
        {
            var notificacao = await _context.Notificacoes
                .FirstOrDefaultAsync(n => n.Id == id && n.ApplicationUserId == userId);
            if (notificacao != null)
            {
                notificacao.Lida = true;
                await _context.SaveChangesAsync();
            }
        }

        public async Task MarcarTodasComoLidasAsync(string userId)
        {
            var naoLidas = await _context.Notificacoes
                .Where(n => n.ApplicationUserId == userId && !n.Lida)
                .ToListAsync();
            foreach (var n in naoLidas) n.Lida = true;
            await _context.SaveChangesAsync();
        }
    }
}
