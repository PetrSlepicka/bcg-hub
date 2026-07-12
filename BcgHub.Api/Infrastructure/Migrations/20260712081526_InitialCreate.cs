using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BcgHub.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BusinessPartners",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    CompanyNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    VatNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    Phone = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Website = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Street = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    City = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PostalCode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    CountryCode = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    Notes = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
                    TransportCapabilities = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessPartners", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContactPeople",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessPartnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Position = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    Phone = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactPeople", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContactPeople_BusinessPartners_BusinessPartnerId",
                        column: x => x.BusinessPartnerId,
                        principalTable: "BusinessPartners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmailAccountSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    ImapServer = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    ImapPort = table.Column<int>(type: "integer", nullable: false),
                    ImapUseSsl = table.Column<bool>(type: "boolean", nullable: false),
                    ImapUsername = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    ProtectedImapPassword = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailAccountSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailAccountSettings_Users_UserAccountId",
                        column: x => x.UserAccountId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PohodaOrderNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerContactId = table.Column<Guid>(type: "uuid", nullable: true),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: true),
                    CarrierId = table.Column<Guid>(type: "uuid", nullable: true),
                    CustomsDeclarantId = table.Column<Guid>(type: "uuid", nullable: true),
                    OrderedOn = table.Column<DateOnly>(type: "date", nullable: true),
                    RequestedDeliveryOn = table.Column<DateOnly>(type: "date", nullable: true),
                    PlannedPickupOn = table.Column<DateOnly>(type: "date", nullable: true),
                    PlannedDeliveryOn = table.Column<DateOnly>(type: "date", nullable: true),
                    ValueCzk = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    WeightKg = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    VolumeM3 = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    WarehouseInstructions = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                    table.CheckConstraint("CK_Orders_NonNegativeValues", "\"ValueCzk\" >= 0 AND \"WeightKg\" >= 0 AND \"VolumeM3\" >= 0");
                    table.ForeignKey(
                        name: "FK_Orders_BusinessPartners_CarrierId",
                        column: x => x.CarrierId,
                        principalTable: "BusinessPartners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Orders_BusinessPartners_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "BusinessPartners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Orders_BusinessPartners_CustomsDeclarantId",
                        column: x => x.CustomsDeclarantId,
                        principalTable: "BusinessPartners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Orders_BusinessPartners_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "BusinessPartners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Orders_ContactPeople_CustomerContactId",
                        column: x => x.CustomerContactId,
                        principalTable: "ContactPeople",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Communications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    BusinessPartnerId = table.Column<Guid>(type: "uuid", nullable: true),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    Subject = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    BodyPreview = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
                    Sender = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Recipients = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExternalProvider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ExternalMailboxId = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ExternalId = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Communications", x => x.Id);
                    table.CheckConstraint("CK_Communications_HasOwner", "\"BusinessPartnerId\" IS NOT NULL OR \"OrderId\" IS NOT NULL");
                    table.ForeignKey(
                        name: "FK_Communications_BusinessPartners_BusinessPartnerId",
                        column: x => x.BusinessPartnerId,
                        principalTable: "BusinessPartners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Communications_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "EmailMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessPartnerId = table.Column<Guid>(type: "uuid", nullable: true),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    Direction = table.Column<int>(type: "integer", nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ImapUid = table.Column<long>(type: "bigint", nullable: false),
                    Mailbox = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    FromAddress = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    FromName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ToAddress = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    CcAddress = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Subject = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    BodyText = table.Column<string>(type: "text", nullable: true),
                    BodyHtml = table.Column<string>(type: "text", nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    HasAttachments = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailMessages_BusinessPartners_BusinessPartnerId",
                        column: x => x.BusinessPartnerId,
                        principalTable: "BusinessPartners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_EmailMessages_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_EmailMessages_Users_UserAccountId",
                        column: x => x.UserAccountId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderWorkflowSteps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    DueAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderWorkflowSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderWorkflowSteps_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TransportQuotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    CarrierId = table.Column<Guid>(type: "uuid", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    PickupOn = table.Column<DateOnly>(type: "date", nullable: true),
                    DeliveryOn = table.Column<DateOnly>(type: "date", nullable: true),
                    IsSelected = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransportQuotes", x => x.Id);
                    table.CheckConstraint("CK_TransportQuotes_NonNegativePrice", "\"Price\" >= 0");
                    table.ForeignKey(
                        name: "FK_TransportQuotes_BusinessPartners_CarrierId",
                        column: x => x.CarrierId,
                        principalTable: "BusinessPartners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransportQuotes_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Attachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessPartnerId = table.Column<Guid>(type: "uuid", nullable: true),
                    ContactPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    WorkflowStepId = table.Column<Guid>(type: "uuid", nullable: true),
                    TransportQuoteId = table.Column<Guid>(type: "uuid", nullable: true),
                    CommunicationId = table.Column<Guid>(type: "uuid", nullable: true),
                    EmailMessageId = table.Column<Guid>(type: "uuid", nullable: true),
                    FileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Size = table.Column<long>(type: "bigint", nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attachments", x => x.Id);
                    table.CheckConstraint("CK_Attachments_ExactlyOneOwner", "num_nonnulls(\"BusinessPartnerId\", \"ContactPersonId\", \"OrderId\", \"WorkflowStepId\", \"TransportQuoteId\", \"CommunicationId\", \"EmailMessageId\") = 1");
                    table.CheckConstraint("CK_Attachments_NonNegativeSize", "\"Size\" >= 0");
                    table.ForeignKey(
                        name: "FK_Attachments_BusinessPartners_BusinessPartnerId",
                        column: x => x.BusinessPartnerId,
                        principalTable: "BusinessPartners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Attachments_Communications_CommunicationId",
                        column: x => x.CommunicationId,
                        principalTable: "Communications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Attachments_ContactPeople_ContactPersonId",
                        column: x => x.ContactPersonId,
                        principalTable: "ContactPeople",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Attachments_EmailMessages_EmailMessageId",
                        column: x => x.EmailMessageId,
                        principalTable: "EmailMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Attachments_OrderWorkflowSteps_WorkflowStepId",
                        column: x => x.WorkflowStepId,
                        principalTable: "OrderWorkflowSteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Attachments_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Attachments_TransportQuotes_TransportQuoteId",
                        column: x => x.TransportQuoteId,
                        principalTable: "TransportQuotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Comments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessPartnerId = table.Column<Guid>(type: "uuid", nullable: true),
                    ContactPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    WorkflowStepId = table.Column<Guid>(type: "uuid", nullable: true),
                    TransportQuoteId = table.Column<Guid>(type: "uuid", nullable: true),
                    CommunicationId = table.Column<Guid>(type: "uuid", nullable: true),
                    EmailMessageId = table.Column<Guid>(type: "uuid", nullable: true),
                    AuthorName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Text = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comments", x => x.Id);
                    table.CheckConstraint("CK_Comments_ExactlyOneOwner", "num_nonnulls(\"BusinessPartnerId\", \"ContactPersonId\", \"OrderId\", \"WorkflowStepId\", \"TransportQuoteId\", \"CommunicationId\", \"EmailMessageId\") = 1");
                    table.ForeignKey(
                        name: "FK_Comments_BusinessPartners_BusinessPartnerId",
                        column: x => x.BusinessPartnerId,
                        principalTable: "BusinessPartners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Comments_Communications_CommunicationId",
                        column: x => x.CommunicationId,
                        principalTable: "Communications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Comments_ContactPeople_ContactPersonId",
                        column: x => x.ContactPersonId,
                        principalTable: "ContactPeople",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Comments_EmailMessages_EmailMessageId",
                        column: x => x.EmailMessageId,
                        principalTable: "EmailMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Comments_OrderWorkflowSteps_WorkflowStepId",
                        column: x => x.WorkflowStepId,
                        principalTable: "OrderWorkflowSteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Comments_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Comments_TransportQuotes_TransportQuoteId",
                        column: x => x.TransportQuoteId,
                        principalTable: "TransportQuotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_BusinessPartnerId_CreatedAtUtc",
                table: "Attachments",
                columns: new[] { "BusinessPartnerId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_CommunicationId_CreatedAtUtc",
                table: "Attachments",
                columns: new[] { "CommunicationId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_ContactPersonId_CreatedAtUtc",
                table: "Attachments",
                columns: new[] { "ContactPersonId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_EmailMessageId_CreatedAtUtc",
                table: "Attachments",
                columns: new[] { "EmailMessageId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_OrderId_CreatedAtUtc",
                table: "Attachments",
                columns: new[] { "OrderId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_TransportQuoteId_CreatedAtUtc",
                table: "Attachments",
                columns: new[] { "TransportQuoteId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_WorkflowStepId_CreatedAtUtc",
                table: "Attachments",
                columns: new[] { "WorkflowStepId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessPartners_Type_Name",
                table: "BusinessPartners",
                columns: new[] { "Type", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_Comments_BusinessPartnerId_CreatedAtUtc",
                table: "Comments",
                columns: new[] { "BusinessPartnerId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Comments_CommunicationId_CreatedAtUtc",
                table: "Comments",
                columns: new[] { "CommunicationId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Comments_ContactPersonId_CreatedAtUtc",
                table: "Comments",
                columns: new[] { "ContactPersonId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Comments_EmailMessageId_CreatedAtUtc",
                table: "Comments",
                columns: new[] { "EmailMessageId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Comments_OrderId_CreatedAtUtc",
                table: "Comments",
                columns: new[] { "OrderId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Comments_TransportQuoteId_CreatedAtUtc",
                table: "Comments",
                columns: new[] { "TransportQuoteId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Comments_WorkflowStepId_CreatedAtUtc",
                table: "Comments",
                columns: new[] { "WorkflowStepId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Communications_BusinessPartnerId_OccurredAtUtc",
                table: "Communications",
                columns: new[] { "BusinessPartnerId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Communications_ExternalProvider_ExternalMailboxId_ExternalId",
                table: "Communications",
                columns: new[] { "ExternalProvider", "ExternalMailboxId", "ExternalId" },
                unique: true,
                filter: "\"ExternalId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Communications_OrderId_OccurredAtUtc",
                table: "Communications",
                columns: new[] { "OrderId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ContactPeople_BusinessPartnerId",
                table: "ContactPeople",
                column: "BusinessPartnerId",
                unique: true,
                filter: "\"IsPrimary\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_ContactPeople_Email",
                table: "ContactPeople",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_EmailAccountSettings_UserAccountId",
                table: "EmailAccountSettings",
                column: "UserAccountId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailMessages_BusinessPartnerId",
                table: "EmailMessages",
                column: "BusinessPartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailMessages_OrderId",
                table: "EmailMessages",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailMessages_UserAccountId_ExternalId",
                table: "EmailMessages",
                columns: new[] { "UserAccountId", "ExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailMessages_UserAccountId_OccurredAtUtc",
                table: "EmailMessages",
                columns: new[] { "UserAccountId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CarrierId",
                table: "Orders",
                column: "CarrierId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CustomerContactId",
                table: "Orders",
                column: "CustomerContactId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CustomerId",
                table: "Orders",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CustomsDeclarantId",
                table: "Orders",
                column: "CustomsDeclarantId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Number",
                table: "Orders",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Status_PlannedDeliveryOn",
                table: "Orders",
                columns: new[] { "Status", "PlannedDeliveryOn" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_WarehouseId",
                table: "Orders",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderWorkflowSteps_OrderId_Type",
                table: "OrderWorkflowSteps",
                columns: new[] { "OrderId", "Type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TransportQuotes_CarrierId",
                table: "TransportQuotes",
                column: "CarrierId");

            migrationBuilder.CreateIndex(
                name: "IX_TransportQuotes_OrderId",
                table: "TransportQuotes",
                column: "OrderId",
                unique: true,
                filter: "\"IsSelected\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Attachments");

            migrationBuilder.DropTable(
                name: "Comments");

            migrationBuilder.DropTable(
                name: "EmailAccountSettings");

            migrationBuilder.DropTable(
                name: "Communications");

            migrationBuilder.DropTable(
                name: "EmailMessages");

            migrationBuilder.DropTable(
                name: "OrderWorkflowSteps");

            migrationBuilder.DropTable(
                name: "TransportQuotes");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "ContactPeople");

            migrationBuilder.DropTable(
                name: "BusinessPartners");
        }
    }
}
