using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BcgHub.Api.Infrastructure.Migrations;

[DbContext(typeof(BcgHubDbContext))]
[Migration("20260712193000_SynchronizePartnerPrimaryContacts")]
public sealed class SynchronizePartnerPrimaryContacts : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE "ContactPeople" AS contact
            SET "Email" = partner."Email", "Phone" = partner."Phone", "UpdatedAtUtc" = NOW()
            FROM "BusinessPartners" AS partner
            WHERE contact."BusinessPartnerId" = partner."Id" AND contact."IsPrimary" = TRUE;

            WITH first_contacts AS (
                SELECT DISTINCT ON (contact."BusinessPartnerId") contact."Id", contact."BusinessPartnerId"
                FROM "ContactPeople" AS contact
                WHERE NOT EXISTS (
                    SELECT 1 FROM "ContactPeople" AS primary_contact
                    WHERE primary_contact."BusinessPartnerId" = contact."BusinessPartnerId" AND primary_contact."IsPrimary" = TRUE)
                ORDER BY contact."BusinessPartnerId", contact."CreatedAtUtc", contact."Id")
            UPDATE "ContactPeople" AS contact
            SET "IsPrimary" = TRUE, "Email" = partner."Email", "Phone" = partner."Phone", "UpdatedAtUtc" = NOW()
            FROM first_contacts, "BusinessPartners" AS partner
            WHERE contact."Id" = first_contacts."Id" AND partner."Id" = first_contacts."BusinessPartnerId";

            INSERT INTO "ContactPeople" ("Id", "BusinessPartnerId", "FullName", "Email", "Phone", "IsPrimary", "CreatedAtUtc", "UpdatedAtUtc")
            SELECT gen_random_uuid(), partner."Id", partner."Name", partner."Email", partner."Phone", TRUE, NOW(), NOW()
            FROM "BusinessPartners" AS partner
            WHERE (partner."Email" IS NOT NULL OR partner."Phone" IS NOT NULL)
                AND NOT EXISTS (SELECT 1 FROM "ContactPeople" AS contact WHERE contact."BusinessPartnerId" = partner."Id");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
