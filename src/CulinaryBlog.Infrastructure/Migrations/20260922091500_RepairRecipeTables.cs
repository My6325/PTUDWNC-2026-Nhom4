using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using CulinaryBlog.Infrastructure.Persistence;

#nullable disable

namespace CulinaryBlog.Infrastructure.Migrations;

/// <inheritdoc />
[DbContext(typeof(ApplicationDbContext))]
[Migration("20260922091500_RepairRecipeTables")]
public partial class RepairRecipeTables : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS "Recipes" (
                "Id" uuid NOT NULL,
                "Title" character varying(200) NOT NULL,
                "Slug" character varying(220) NOT NULL,
                "Description" text NULL,
                "Instructions" text NULL,
                "PrepTimeMinutes" integer NOT NULL,
                "CookTimeMinutes" integer NOT NULL,
                "Servings" integer NOT NULL,
                "Difficulty" integer NOT NULL,
                "Status" integer NOT NULL,
                "CategoryId" uuid NULL,
                "AuthorId" character varying(450) NOT NULL,
                "Nutrition_Calories" integer NULL,
                "Nutrition_Protein" numeric(10,2) NULL,
                "Nutrition_Carbohydrates" numeric(10,2) NULL,
                "Nutrition_Fat" numeric(10,2) NULL,
                "Nutrition_Fiber" numeric(10,2) NULL,
                "Nutrition_Sodium" numeric(10,2) NULL,
                "CreatedAt" timestamp with time zone NOT NULL,
                "UpdatedAt" timestamp with time zone NULL,
                "IsDeleted" boolean NOT NULL,
                "DeletedAt" timestamp with time zone NULL,
                CONSTRAINT "PK_Recipes" PRIMARY KEY ("Id"),
                CONSTRAINT "FK_Recipes_Categories_CategoryId" FOREIGN KEY ("CategoryId") REFERENCES "Categories" ("Id") ON DELETE SET NULL
            );

            CREATE TABLE IF NOT EXISTS "RecipeImages" (
                "Id" uuid NOT NULL,
                "RecipeId" uuid NOT NULL,
                "OriginalUrl" text NOT NULL,
                "MediumUrl" text NULL,
                "ThumbnailUrl" text NULL,
                "IsPrimary" boolean NOT NULL,
                "OrderIndex" integer NOT NULL,
                "CreatedAt" timestamp with time zone NOT NULL,
                "UpdatedAt" timestamp with time zone NULL,
                "IsDeleted" boolean NOT NULL,
                "DeletedAt" timestamp with time zone NULL,
                "RowVersion" bigint NOT NULL DEFAULT 0,
                CONSTRAINT "PK_RecipeImages" PRIMARY KEY ("Id"),
                CONSTRAINT "FK_RecipeImages_Recipes_RecipeId" FOREIGN KEY ("RecipeId") REFERENCES "Recipes" ("Id") ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS "RecipeIngredients" (
                "Id" uuid NOT NULL,
                "RecipeId" uuid NOT NULL,
                "Name" text NOT NULL,
                "Quantity" numeric NULL,
                "Unit" text NULL,
                "Notes" text NULL,
                "OrderIndex" integer NOT NULL,
                "CreatedAt" timestamp with time zone NOT NULL,
                "UpdatedAt" timestamp with time zone NULL,
                "IsDeleted" boolean NOT NULL,
                "DeletedAt" timestamp with time zone NULL,
                "RowVersion" bigint NOT NULL DEFAULT 0,
                CONSTRAINT "PK_RecipeIngredients" PRIMARY KEY ("Id"),
                CONSTRAINT "FK_RecipeIngredients_Recipes_RecipeId" FOREIGN KEY ("RecipeId") REFERENCES "Recipes" ("Id") ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS "RecipeSteps" (
                "Id" uuid NOT NULL,
                "RecipeId" uuid NOT NULL,
                "StepNumber" integer NOT NULL,
                "Title" text NOT NULL,
                "Description" text NOT NULL,
                "TimerMinutes" integer NULL,
                "ImageUrl" text NULL,
                "CreatedAt" timestamp with time zone NOT NULL,
                "UpdatedAt" timestamp with time zone NULL,
                "IsDeleted" boolean NOT NULL,
                "DeletedAt" timestamp with time zone NULL,
                "RowVersion" bigint NOT NULL DEFAULT 0,
                CONSTRAINT "PK_RecipeSteps" PRIMARY KEY ("Id"),
                CONSTRAINT "FK_RecipeSteps_Recipes_RecipeId" FOREIGN KEY ("RecipeId") REFERENCES "Recipes" ("Id") ON DELETE CASCADE
            );

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_Recipes_Slug" ON "Recipes" ("Slug");
            CREATE INDEX IF NOT EXISTS "IX_Recipes_CategoryId" ON "Recipes" ("CategoryId");
            CREATE INDEX IF NOT EXISTS "IX_RecipeImages_RecipeId" ON "RecipeImages" ("RecipeId");
            CREATE INDEX IF NOT EXISTS "IX_RecipeIngredients_RecipeId" ON "RecipeIngredients" ("RecipeId");
            CREATE INDEX IF NOT EXISTS "IX_RecipeSteps_RecipeId" ON "RecipeSteps" ("RecipeId");
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Repair is additive and may reuse pre-existing tables. A rollback cannot
        // distinguish them from newly created tables, so preserve all data.
    }
}
