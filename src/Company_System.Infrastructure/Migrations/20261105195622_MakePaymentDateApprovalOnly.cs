using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HR_System.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakePaymentDateApprovalOnly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateOnly>(
                name: "PaymentDate",
                table: "Payments",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateOnly),
                oldType: "date");

            migrationBuilder.Sql("UPDATE [Payments] SET [PaymentDate] = NULL WHERE [State] <> 1;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE [Payments] SET [PaymentDate] = DATEFROMPARTS([Year], [Month], 1) WHERE [PaymentDate] IS NULL;");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "PaymentDate",
                table: "Payments",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);
        }
    }
}
