namespace TestSite.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Initialize : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Addresses",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Street = c.String(),
                        Zip = c.String(),
                        City = c.String(),
                        Country = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.Associates",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(),
                        Role = c.String(),
                        AddressId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Addresses", t => t.AddressId)
                .Index(t => t.AddressId);
            
            CreateTable(
                "dbo.Insurances",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(),
                        Type = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.PatientInsurances",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        InsuranceNumber = c.String(),
                        PatientId = c.Int(nullable: false),
                        InsuranceId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Insurances", t => t.InsuranceId)
                .ForeignKey("dbo.Patients", t => t.PatientId)
                .Index(t => t.PatientId)
                .Index(t => t.InsuranceId);
            
            CreateTable(
                "dbo.Patients",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        FirstName = c.String(),
                        LastName = c.String(),
                        AddressId = c.Int(nullable: false),
                        AssociateId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Addresses", t => t.AddressId)
                .ForeignKey("dbo.Associates", t => t.AssociateId)
                .Index(t => t.AddressId)
                .Index(t => t.AssociateId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.PatientInsurances", "PatientId", "dbo.Patients");
            DropForeignKey("dbo.Patients", "AssociateId", "dbo.Associates");
            DropForeignKey("dbo.Patients", "AddressId", "dbo.Addresses");
            DropForeignKey("dbo.PatientInsurances", "InsuranceId", "dbo.Insurances");
            DropForeignKey("dbo.Associates", "AddressId", "dbo.Addresses");
            DropIndex("dbo.Patients", new[] { "AssociateId" });
            DropIndex("dbo.Patients", new[] { "AddressId" });
            DropIndex("dbo.PatientInsurances", new[] { "InsuranceId" });
            DropIndex("dbo.PatientInsurances", new[] { "PatientId" });
            DropIndex("dbo.Associates", new[] { "AddressId" });
            DropTable("dbo.Patients");
            DropTable("dbo.PatientInsurances");
            DropTable("dbo.Insurances");
            DropTable("dbo.Associates");
            DropTable("dbo.Addresses");
        }
    }
}
