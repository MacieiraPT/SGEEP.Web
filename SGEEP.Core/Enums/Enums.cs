namespace SGEEP.Core.Enums
{
    public enum TipoUtilizador
    {
        Administrador,
        Professor,
        Aluno,
        Empresa
    }

    public enum EstadoEstagio
    {
        Pendente,
        Ativo,
        Concluido,
        Cancelado
    }

    public enum EstadoRelatorio
    {
        Rascunho,
        Submetido,
        EmRevisao,
        Aprovado,
        Rejeitado
    }

    public enum EstadoHoras
    {
        Pendente,
        Validado,
        Rejeitado
    }
}