using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GarageOS.Execucao.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InicialFase4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Estoques",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Quantidade = table.Column<int>(type: "integer", nullable: false),
                    QuantidadeReservada = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    Valor = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DataEntrada = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DataSaida = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Fornecedor = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Estoques", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrdensDeExecucao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrdemDeServicoId = table.Column<Guid>(type: "uuid", nullable: false),
                    NumeroOS = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FilialId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MotivoFalha = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CriadaEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadaEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrdensDeExecucao", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReservasPecas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrdemDeServicoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CriadaEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadaEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReservasPecas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HistoricoExecucao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrdemDeExecucaoId = table.Column<Guid>(type: "uuid", nullable: false),
                    StatusAnterior = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    StatusNovo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Motivo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    OcorridoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoricoExecucao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistoricoExecucao_OrdensDeExecucao_OrdemDeExecucaoId",
                        column: x => x.OrdemDeExecucaoId,
                        principalTable: "OrdensDeExecucao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItensExecucao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrdemDeExecucaoId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServicoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IniciadaEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FinalizadaEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItensExecucao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItensExecucao_OrdensDeExecucao_OrdemDeExecucaoId",
                        column: x => x.OrdemDeExecucaoId,
                        principalTable: "OrdensDeExecucao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItensReserva",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReservaPecasId = table.Column<Guid>(type: "uuid", nullable: false),
                    EstoqueId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantidade = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItensReserva", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItensReserva_Estoques_EstoqueId",
                        column: x => x.EstoqueId,
                        principalTable: "Estoques",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ItensReserva_ReservasPecas_ReservaPecasId",
                        column: x => x.ReservaPecasId,
                        principalTable: "ReservasPecas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HistoricoExecucao_OrdemDeExecucaoId",
                table: "HistoricoExecucao",
                column: "OrdemDeExecucaoId");

            migrationBuilder.CreateIndex(
                name: "IX_ItensExecucao_OrdemDeExecucaoId",
                table: "ItensExecucao",
                column: "OrdemDeExecucaoId");

            migrationBuilder.CreateIndex(
                name: "IX_ItensExecucao_ServicoId_FinalizadaEm",
                table: "ItensExecucao",
                columns: new[] { "ServicoId", "FinalizadaEm" });

            migrationBuilder.CreateIndex(
                name: "IX_ItensReserva_EstoqueId",
                table: "ItensReserva",
                column: "EstoqueId");

            migrationBuilder.CreateIndex(
                name: "IX_ItensReserva_ReservaPecasId",
                table: "ItensReserva",
                column: "ReservaPecasId");

            migrationBuilder.CreateIndex(
                name: "IX_OrdensDeExecucao_OrdemDeServicoId",
                table: "OrdensDeExecucao",
                column: "OrdemDeServicoId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrdensDeExecucao_Status",
                table: "OrdensDeExecucao",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ReservasPecas_OrdemDeServicoId",
                table: "ReservasPecas",
                column: "OrdemDeServicoId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HistoricoExecucao");

            migrationBuilder.DropTable(
                name: "ItensExecucao");

            migrationBuilder.DropTable(
                name: "ItensReserva");

            migrationBuilder.DropTable(
                name: "OrdensDeExecucao");

            migrationBuilder.DropTable(
                name: "Estoques");

            migrationBuilder.DropTable(
                name: "ReservasPecas");
        }
    }
}
