using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyFinances.Migrations
{
    /// <inheritdoc />
    public partial class ReclassificarStatusFaturaPorCalendario : Migration
    {
        // Reclassifica o status das faturas pelo calendario (item 12, "Ciclo e status da fatura").
        // Roda dentro da transacao da migration; as tabelas temporarias sao derrubadas explicitamente
        // ao final (nao depende de ON COMMIT DROP). Idempotente: rodar de novo chega ao mesmo estado.
        // Aborta se houver duplicatas (conta_id, data_fechamento) com mais de uma PAGA (decisao manual).

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM fatura
                        GROUP BY conta_id, data_fechamento
                        HAVING COUNT(*) FILTER (WHERE status = 'PAGA') > 1
                    ) THEN
                        RAISE EXCEPTION 'Duplicatas de fatura com mais de uma PAGA para o mesmo conta_id/data_fechamento. Migration abortada: decisao manual.';
                    END IF;
                END $$;
            ");

            migrationBuilder.Sql(@"
                DROP TABLE IF EXISTS fatura_consolidacao;
                CREATE TEMP TABLE fatura_consolidacao AS
                SELECT
                    f.id,
                    f.conta_id,
                    f.data_fechamento,
                    ROW_NUMBER() OVER (
                        PARTITION BY f.conta_id, f.data_fechamento
                        ORDER BY (f.status = 'PAGA') DESC,
                                 ((SELECT COUNT(*) FROM lancamento l WHERE l.fatura_id = f.id)
                                  + (SELECT COUNT(*) FROM transferencia t WHERE t.fatura_id = f.id)) DESC,
                                 f.id
                    ) AS posicao
                FROM fatura f;

                DROP TABLE IF EXISTS fatura_mapeamento;
                CREATE TEMP TABLE fatura_mapeamento AS
                SELECT perdedora.id AS de, mantida.id AS para
                FROM fatura_consolidacao perdedora
                JOIN fatura_consolidacao mantida
                  ON mantida.conta_id = perdedora.conta_id
                 AND mantida.data_fechamento = perdedora.data_fechamento
                 AND mantida.posicao = 1
                WHERE perdedora.posicao > 1;

                UPDATE lancamento l
                SET fatura_id = m.para
                FROM fatura_mapeamento m
                WHERE l.fatura_id = m.de;

                UPDATE transferencia t
                SET fatura_id = m.para
                FROM fatura_mapeamento m
                WHERE t.fatura_id = m.de;

                DELETE FROM fatura f
                USING fatura_mapeamento m
                WHERE f.id = m.de;
            ");

            migrationBuilder.Sql(@"
                UPDATE fatura SET status = 'FUTURA' WHERE status IN ('ABERTA', 'FECHADA');
            ");

            migrationBuilder.Sql(@"
                UPDATE fatura SET status = 'FECHADA'
                WHERE status = 'FUTURA' AND data_fechamento < CURRENT_DATE;
            ");

            migrationBuilder.Sql(@"
                DROP TABLE IF EXISTS ciclo_atual_conta;
                CREATE TEMP TABLE ciclo_atual_conta AS
                WITH meses AS (
                    SELECT
                        date_trunc('month', CURRENT_DATE)::date AS m0,
                        (date_trunc('month', CURRENT_DATE) + INTERVAL '1 month')::date AS m1
                ),
                candidatos AS (
                    SELECT
                        c.id AS conta_id,
                        (m.m0 + ((LEAST(COALESCE(c.dia_fechamento, 1),
                                        EXTRACT(DAY FROM (m.m0 + INTERVAL '1 month - 1 day'))::int) - 1)
                                  * INTERVAL '1 day'))::date AS fech_mes_corrente,
                        (m.m1 + ((LEAST(COALESCE(c.dia_fechamento, 1),
                                        EXTRACT(DAY FROM (m.m1 + INTERVAL '1 month - 1 day'))::int) - 1)
                                  * INTERVAL '1 day'))::date AS fech_proximo_mes
                    FROM conta c
                    CROSS JOIN meses m
                    WHERE c.tipo = 'CARTAO'
                )
                SELECT
                    conta_id,
                    CASE WHEN fech_mes_corrente >= CURRENT_DATE
                         THEN fech_mes_corrente
                         ELSE fech_proximo_mes
                    END AS fech_atual
                FROM candidatos;

                UPDATE fatura f
                SET status = 'ABERTA'
                FROM (
                    SELECT DISTINCT ON (f2.conta_id) f2.id
                    FROM fatura f2
                    JOIN ciclo_atual_conta cc ON cc.conta_id = f2.conta_id
                    WHERE f2.data_fechamento BETWEEN CURRENT_DATE AND cc.fech_atual
                    ORDER BY f2.conta_id, f2.data_fechamento
                ) atual
                WHERE f.id = atual.id
                  AND f.status <> 'PAGA';

                DROP TABLE IF EXISTS ciclo_atual_conta;
                DROP TABLE IF EXISTS fatura_mapeamento;
                DROP TABLE IF EXISTS fatura_consolidacao;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Nao restaura as duplicatas consolidadas no Up (lancamentos ja foram movidos e as faturas apagadas).
            migrationBuilder.Sql(@"
                UPDATE fatura SET status = 'FECHADA' WHERE status = 'FUTURA';
            ");
        }
    }
}
