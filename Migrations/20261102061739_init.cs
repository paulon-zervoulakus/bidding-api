using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace biddingServer.Migrations
{
    /// <inheritdoc />
    public partial class init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Password = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastLoggedIn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsLoggedIn = table.Column<bool>(type: "bit", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    Gender = table.Column<int>(type: "int", nullable: false),
                    RefreshToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RefreshTokenExpiry = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ParentCategoryId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductCategories_ProductCategories_ParentCategoryId",
                        column: x => x.ParentCategoryId,
                        principalTable: "ProductCategories",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AuctionRooms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GroupName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    HostId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuctionRooms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuctionRooms_Accounts_HostId",
                        column: x => x.HostId,
                        principalTable: "Accounts",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    Price = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    SKU = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    SellerID = table.Column<int>(type: "int", nullable: false),
                    IsSerializable = table.Column<bool>(type: "bit", nullable: false),
                    ProductCondition = table.Column<int>(type: "int", nullable: false),
                    ProductCategoryID = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Products_Accounts_SellerID",
                        column: x => x.SellerID,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Products_ProductCategories_ProductCategoryID",
                        column: x => x.ProductCategoryID,
                        principalTable: "ProductCategories",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AuctionRoomMembers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AuctionRoomId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuctionRoomMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuctionRoomMembers_Accounts_UserId",
                        column: x => x.UserId,
                        principalTable: "Accounts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AuctionRoomMembers_AuctionRooms_AuctionRoomId",
                        column: x => x.AuctionRoomId,
                        principalTable: "AuctionRooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AuctionRoomProducts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    StartingBidPrice = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    LastBidPrice = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    MinimumBidIncrement = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    StartingBidTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndBidTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CurrentHighBidderID = table.Column<int>(type: "int", nullable: true),
                    NumberOfBids = table.Column<int>(type: "int", nullable: true),
                    AuctionRoomId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuctionRoomProducts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuctionRoomProducts_Accounts_CurrentHighBidderID",
                        column: x => x.CurrentHighBidderID,
                        principalTable: "Accounts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AuctionRoomProducts_AuctionRooms_AuctionRoomId",
                        column: x => x.AuctionRoomId,
                        principalTable: "AuctionRooms",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AuctionRoomProducts_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ProductImages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    OriginalSize = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SquareSize = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ThumbnailSize = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IconSize = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductImages_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductSerials",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SerialNumber = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductSerials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductSerials_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AuctionRoomProductTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AuctionRoomProductId = table.Column<int>(type: "int", nullable: false),
                    Bid = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    CurrentBidderID = table.Column<int>(type: "int", nullable: false),
                    BidTimestamp = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuctionRoomProductTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuctionRoomProductTransactions_Accounts_CurrentBidderID",
                        column: x => x.CurrentBidderID,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AuctionRoomProductTransactions_AuctionRoomProducts_AuctionRoomProductId",
                        column: x => x.AuctionRoomProductId,
                        principalTable: "AuctionRoomProducts",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuctionRoomMembers_AuctionRoomId",
                table: "AuctionRoomMembers",
                column: "AuctionRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_AuctionRoomMembers_UserId",
                table: "AuctionRoomMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AuctionRoomProducts_AuctionRoomId",
                table: "AuctionRoomProducts",
                column: "AuctionRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_AuctionRoomProducts_CurrentHighBidderID",
                table: "AuctionRoomProducts",
                column: "CurrentHighBidderID");

            migrationBuilder.CreateIndex(
                name: "IX_AuctionRoomProducts_ProductId",
                table: "AuctionRoomProducts",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_AuctionRoomProductTransactions_AuctionRoomProductId",
                table: "AuctionRoomProductTransactions",
                column: "AuctionRoomProductId");

            migrationBuilder.CreateIndex(
                name: "IX_AuctionRoomProductTransactions_CurrentBidderID",
                table: "AuctionRoomProductTransactions",
                column: "CurrentBidderID");

            migrationBuilder.CreateIndex(
                name: "IX_AuctionRooms_HostId",
                table: "AuctionRooms",
                column: "HostId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductCategories_ParentCategoryId",
                table: "ProductCategories",
                column: "ParentCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductImages_ProductId",
                table: "ProductImages",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_ProductCategoryID",
                table: "Products",
                column: "ProductCategoryID");

            migrationBuilder.CreateIndex(
                name: "IX_Products_SellerID",
                table: "Products",
                column: "SellerID");

            migrationBuilder.CreateIndex(
                name: "IX_ProductSerials_ProductId",
                table: "ProductSerials",
                column: "ProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuctionRoomMembers");

            migrationBuilder.DropTable(
                name: "AuctionRoomProductTransactions");

            migrationBuilder.DropTable(
                name: "ProductImages");

            migrationBuilder.DropTable(
                name: "ProductSerials");

            migrationBuilder.DropTable(
                name: "AuctionRoomProducts");

            migrationBuilder.DropTable(
                name: "AuctionRooms");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "Accounts");

            migrationBuilder.DropTable(
                name: "ProductCategories");
        }
    }
}
