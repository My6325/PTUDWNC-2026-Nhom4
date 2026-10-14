using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using CulinaryBlog.Infrastructure.Persistence;

#nullable disable

namespace CulinaryBlog.Infrastructure.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261006120000_AddRecipeSearchAndPublication")]
public partial class AddRecipeSearchAndPublication : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("CREATE OR REPLACE FUNCTION public.recipe_unaccent(text) RETURNS text LANGUAGE sql IMMUTABLE PARALLEL SAFE STRICT AS $$ SELECT public.unaccent('public.unaccent'::regdictionary, $1) $$;");
        migrationBuilder.AddColumn<DateTime>(name: "PublishedAt", table: "Recipes", type: "timestamp with time zone", nullable: true);
        migrationBuilder.AddColumn<NpgsqlTypes.NpgsqlTsVector>(name: "SearchVector", table: "Recipes", type: "tsvector", nullable: false,
            computedColumnSql: "setweight(to_tsvector('simple', public.recipe_unaccent(coalesce(\"Title\", ''))), 'A') || setweight(to_tsvector('simple', public.recipe_unaccent(coalesce(\"Description\", ''))), 'B')", stored: true);
        migrationBuilder.CreateIndex(name: "IX_Recipes_SearchVector_GIN", table: "Recipes", column: "SearchVector").Annotation("Npgsql:IndexMethod", "GIN");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_Recipes_SearchVector_GIN", table: "Recipes");
        migrationBuilder.DropColumn(name: "SearchVector", table: "Recipes");
        migrationBuilder.DropColumn(name: "PublishedAt", table: "Recipes");
        migrationBuilder.Sql("DROP FUNCTION IF EXISTS public.recipe_unaccent(text);");
    }
}
