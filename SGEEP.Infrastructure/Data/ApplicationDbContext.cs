using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SGEEP.Core.Entities;

namespace SGEEP.Infrastructure.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Curso> Cursos { get; set; }
        public DbSet<Aluno> Alunos { get; set; }
        public DbSet<Professor> Professores { get; set; }
        public DbSet<Empresa> Empresas { get; set; }
        public DbSet<Estagio> Estagios { get; set; }
        public DbSet<RegistoHoras> RegistoHoras { get; set; }
        public DbSet<Relatorio> Relatorios { get; set; }
        public DbSet<Avaliacao> Avaliacoes { get; set; }
        public DbSet<Notificacao> Notificacoes { get; set; }
        public DbSet<RegistoAuditoria> RegistosAuditoria { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Aluno → Estágio (1:N)
            builder.Entity<Aluno>()
                .HasMany(a => a.Estagios)
                .WithOne(e => e.Aluno)
                .HasForeignKey(e => e.AlunoId);

            // Professor → Estágios (1:N)
            builder.Entity<Professor>()
                .HasMany(p => p.Estagios)
                .WithOne(e => e.Professor)
                .HasForeignKey(e => e.ProfessorId);

            // Empresa → Estágios (1:N)
            builder.Entity<Empresa>()
                .HasMany(e => e.Estagios)
                .WithOne(e => e.Empresa)
                .HasForeignKey(e => e.EmpresaId);

            // Estágio → RegistoHoras (1:N)
            builder.Entity<Estagio>()
                .HasMany(e => e.RegistoHoras)
                .WithOne(r => r.Estagio)
                .HasForeignKey(r => r.EstagioId);

            // Estágio → Relatórios (1:N)
            builder.Entity<Estagio>()
                .HasMany(e => e.Relatorios)
                .WithOne(r => r.Estagio)
                .HasForeignKey(r => r.EstagioId);

            // Estágio → Avaliação (1:1)
            builder.Entity<Estagio>()
                .HasOne(e => e.Avaliacao)
                .WithOne(a => a.Estagio)
                .HasForeignKey<Avaliacao>(a => a.EstagioId);

            // Curso → Alunos (1:N)
            builder.Entity<Curso>()
                .HasMany(c => c.Alunos)
                .WithOne(a => a.Curso)
                .HasForeignKey(a => a.CursoId);

            // Curso → Professores (1:N)
            builder.Entity<Curso>()
                .HasMany(c => c.Professores)
                .WithOne(p => p.Curso)
                .HasForeignKey(p => p.CursoId);

            // Empresa - NIF único
            builder.Entity<Empresa>()
                .HasIndex(e => e.NIF)
                .IsUnique();

            // Seed dos 8 cursos
            builder.Entity<Curso>().HasData(
                new Curso { Id = 1, Nome = "Programador/a de Informática", Codigo = "PRG", Ativo = true },
                new Curso { Id = 2, Nome = "Técnico de Audiovisuais", Codigo = "TAV", Ativo = true },
                new Curso { Id = 3, Nome = "Técnico de Desporto", Codigo = "TD", Ativo = true },
                new Curso { Id = 4, Nome = "Técnico de Eletrónica, Automação e Computadores", Codigo = "TEA", Ativo = true },
                new Curso { Id = 5, Nome = "Técnico de Mecatrónica Automóvel", Codigo = "TMA", Ativo = true },
                new Curso { Id = 6, Nome = "Técnico de Turismo", Codigo = "TUR", Ativo = true },
                new Curso { Id = 7, Nome = "Técnico/a de Ação Educativa", Codigo = "TAE", Ativo = true },
                new Curso { Id = 8, Nome = "Técnico/a de Informática – Sistemas", Codigo = "TIS", Ativo = true }
            );
        }
    }
}