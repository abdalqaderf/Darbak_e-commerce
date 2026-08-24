using Darbak.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darbak.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260824212500_AddTestimonialRating")]
    public partial class AddTestimonialRating : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Rating",
                table: "Testimonials",
                type: "int",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Rating",
                table: "Testimonials");
        }
    }
}
