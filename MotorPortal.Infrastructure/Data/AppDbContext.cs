using Microsoft.EntityFrameworkCore;
using MotorPortal.Domain.Entities;

namespace MotorPortal.Infrastructure.Data;

public class AppDbContext : DbContext
{
    private const string Schema = "SGInsurance";

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<UserMaster> UserMasters => Set<UserMaster>();
    public DbSet<ProductMaster> ProductMasters => Set<ProductMaster>();
    public DbSet<FunctionMaster> FunctionMasters => Set<FunctionMaster>();
    public DbSet<MasterPolicy> MasterPolicies => Set<MasterPolicy>();
    public DbSet<BatchMaster> BatchMasters => Set<BatchMaster>();
    public DbSet<BatchDetail> BatchDetails => Set<BatchDetail>();
    public DbSet<InvalidRecord> InvalidRecords => Set<InvalidRecord>();
    public DbSet<PremiumDetails> PremiumDetails => Set<PremiumDetails>();
    public DbSet<GstDetails> GstDetails => Set<GstDetails>();
    public DbSet<ProposalMaster> ProposalMasters => Set<ProposalMaster>();
    public DbSet<PaymentDetails> PaymentDetails => Set<PaymentDetails>();
    public DbSet<PolicyMaster> PolicyMasters => Set<PolicyMaster>();
    public DbSet<PolicyCertificate> PolicyCertificates => Set<PolicyCertificate>();
    public DbSet<ReportLog> ReportLogs => Set<ReportLog>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<UserMaster>(e =>
        {
            e.ToTable("user_master", Schema);
            e.HasKey(x => x.UserId);
            e.Property(x => x.UserId).HasColumnName("user_id").UseIdentityByDefaultColumn();
            e.Property(x => x.Username).HasColumnName("username").IsRequired();
            e.HasIndex(x => x.Username).IsUnique();
            e.Property(x => x.PasswordHash).HasColumnName("password_hash").IsRequired();
            e.Property(x => x.Role).HasColumnName("role").IsRequired();
            e.Property(x => x.Status).HasColumnName("status").HasColumnType("char(1)").IsRequired();
            e.Property(x => x.CreatedOn).HasColumnName("created_on").IsRequired();
        });

        modelBuilder.Entity<ProductMaster>(e =>
        {
            e.ToTable("product_master", Schema);
            e.HasKey(x => x.ProductId);
            e.Property(x => x.ProductId).HasColumnName("product_id").UseIdentityByDefaultColumn();
            e.Property(x => x.ProductCode).HasColumnName("product_code").IsRequired();
            e.HasIndex(x => x.ProductCode).IsUnique();
            e.Property(x => x.ProductName).HasColumnName("product_name").IsRequired();
            e.Property(x => x.Status).HasColumnName("status").HasColumnType("char(1)").IsRequired();
        });

        modelBuilder.Entity<FunctionMaster>(e =>
        {
            e.ToTable("function_master", Schema);
            e.HasKey(x => x.FunctionId);
            e.Property(x => x.FunctionId).HasColumnName("function_id").UseIdentityByDefaultColumn();
            e.Property(x => x.FunctionCode).HasColumnName("function_code").IsRequired();
            e.HasIndex(x => x.FunctionCode).IsUnique();
            e.Property(x => x.FunctionName).HasColumnName("function_name").IsRequired();
        });

        modelBuilder.Entity<MasterPolicy>(e =>
        {
            e.ToTable("master_policy", Schema);
            e.HasKey(x => x.MasterPolicyId);
            e.Property(x => x.MasterPolicyId).HasColumnName("master_policy_id").UseIdentityByDefaultColumn();
            e.Property(x => x.ProductId).HasColumnName("product_id").IsRequired();
            e.Property(x => x.MasterPolicyNo).HasColumnName("master_policy_no").IsRequired();
            e.HasIndex(x => x.MasterPolicyNo).IsUnique();
            e.Property(x => x.CustomerNo).HasColumnName("customer_no").IsRequired();
            e.Property(x => x.CdbgNo).HasColumnName("cdbg_no");
            e.Property(x => x.CdBalance).HasColumnName("cd_balance").HasColumnType("numeric(15,2)").IsRequired();
            e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<BatchMaster>(e =>
        {
            e.ToTable("batch_master", Schema);
            e.HasKey(x => x.BatchId);
            e.Property(x => x.BatchId).HasColumnName("batch_id").UseIdentityByDefaultColumn();
            e.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
            e.Property(x => x.ProductId).HasColumnName("product_id").IsRequired();
            e.Property(x => x.FunctionId).HasColumnName("function_id").IsRequired();
            e.Property(x => x.FileName).HasColumnName("file_name").IsRequired();
            e.Property(x => x.TotalRecords).HasColumnName("total_records").IsRequired();
            e.Property(x => x.ValidRecords).HasColumnName("valid_records").IsRequired();
            e.Property(x => x.InvalidRecords).HasColumnName("invalid_records").IsRequired();
            e.Property(x => x.Status).HasColumnName("status").IsRequired();
            e.Property(x => x.CreatedOn).HasColumnName("created_on").IsRequired();
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Function).WithMany().HasForeignKey(x => x.FunctionId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<BatchDetail>(e =>
        {
            e.ToTable("batch_detail", Schema);
            e.HasKey(x => x.DetailId);
            e.Property(x => x.DetailId).HasColumnName("detail_id").UseIdentityByDefaultColumn();
            e.Property(x => x.BatchId).HasColumnName("batch_id").IsRequired();
            e.Property(x => x.MasterPolicyNo).HasColumnName("master_policy_no").IsRequired();
            e.Property(x => x.EngineNo).HasColumnName("engine_no").IsRequired();
            e.Property(x => x.ChassisNo).HasColumnName("chassis_no").IsRequired();
            e.Property(x => x.TcNo).HasColumnName("tc_no");
            e.Property(x => x.InvoiceNo).HasColumnName("invoice_no");
            e.Property(x => x.TransitDate).HasColumnName("transit_date");
            e.Property(x => x.Make).HasColumnName("make");
            e.Property(x => x.Model).HasColumnName("model");
            e.Property(x => x.RecordStatus).HasColumnName("record_status").IsRequired();
            e.HasOne(x => x.Batch).WithMany(b => b.BatchDetails).HasForeignKey(x => x.BatchId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InvalidRecord>(e =>
        {
            e.ToTable("invalid_records", Schema);
            e.HasKey(x => x.InvalidId);
            e.Property(x => x.InvalidId).HasColumnName("invalid_id").UseIdentityByDefaultColumn();
            e.Property(x => x.BatchId).HasColumnName("batch_id").IsRequired();
            e.Property(x => x.DetailId).HasColumnName("detail_id").IsRequired();
            e.Property(x => x.TransitDate).HasColumnName("transit_date");
            e.Property(x => x.InvoiceNo).HasColumnName("invoice_no");
            e.Property(x => x.EngineNo).HasColumnName("engine_no");
            e.Property(x => x.ChassisNo).HasColumnName("chassis_no");
            e.Property(x => x.ErrorRemarks).HasColumnName("error_remarks").IsRequired();
            e.HasOne(x => x.Batch).WithMany().HasForeignKey(x => x.BatchId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Detail).WithMany().HasForeignKey(x => x.DetailId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PremiumDetails>(e =>
        {
            e.ToTable("premium_details", Schema);
            e.HasKey(x => x.PremiumId);
            e.Property(x => x.PremiumId).HasColumnName("premium_id").UseIdentityByDefaultColumn();
            e.Property(x => x.DetailId).HasColumnName("detail_id").IsRequired();
            e.Property(x => x.BasePremium).HasColumnName("base_premium").IsRequired();
            e.Property(x => x.AddonPremium).HasColumnName("addon_premium").IsRequired();
            e.Property(x => x.Discount).HasColumnName("discount").IsRequired();
            e.Property(x => x.NetPremium).HasColumnName("net_premium").IsRequired();
            e.HasOne(x => x.Detail).WithMany().HasForeignKey(x => x.DetailId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<GstDetails>(e =>
        {
            e.ToTable("gst_details", Schema);
            e.HasKey(x => x.GstId);
            e.Property(x => x.GstId).HasColumnName("gst_id").UseIdentityByDefaultColumn();
            e.Property(x => x.PremiumId).HasColumnName("premium_id").IsRequired();
            e.HasIndex(x => x.PremiumId).IsUnique();
            e.Property(x => x.GstRate).HasColumnName("gst_rate").IsRequired();
            e.Property(x => x.GstAmount).HasColumnName("gst_amount").IsRequired();
            e.Property(x => x.FinalPremium).HasColumnName("final_premium").IsRequired();
            e.HasOne(x => x.Premium).WithOne(p => p.GstDetails).HasForeignKey<GstDetails>(x => x.PremiumId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProposalMaster>(e =>
        {
            e.ToTable("proposal_master", Schema);
            e.HasKey(x => x.ProposalId);
            e.Property(x => x.ProposalId).HasColumnName("proposal_id").UseIdentityByDefaultColumn();
            e.Property(x => x.DetailId).HasColumnName("detail_id").IsRequired();
            e.Property(x => x.ProposalNo).HasColumnName("proposal_no").IsRequired();
            e.HasIndex(x => x.ProposalNo).IsUnique();
            e.Property(x => x.PremiumAmount).HasColumnName("premium_amount").IsRequired();
            e.Property(x => x.Status).HasColumnName("status").IsRequired();
            e.Property(x => x.Remarks).HasColumnName("remarks");
            e.HasOne(x => x.Detail).WithMany().HasForeignKey(x => x.DetailId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PaymentDetails>(e =>
        {
            e.ToTable("payment_details", Schema);
            e.HasKey(x => x.PaymentId);
            e.Property(x => x.PaymentId).HasColumnName("payment_id").UseIdentityByDefaultColumn();
            e.Property(x => x.ProposalId).HasColumnName("proposal_id").IsRequired();
            e.Property(x => x.MasterPolicyId).HasColumnName("master_policy_id").IsRequired();
            e.Property(x => x.PfRefNo).HasColumnName("pf_ref_no");
            e.Property(x => x.Amount).HasColumnName("amount").IsRequired();
            e.Property(x => x.PaymentStatus).HasColumnName("payment_status").IsRequired();
            e.Property(x => x.TaggedOn).HasColumnName("tagged_on");
            e.HasOne(x => x.Proposal).WithMany().HasForeignKey(x => x.ProposalId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.MasterPolicy).WithMany().HasForeignKey(x => x.MasterPolicyId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PolicyMaster>(e =>
        {
            e.ToTable("policy_master", Schema);
            e.HasKey(x => x.PolicyId);
            e.Property(x => x.PolicyId).HasColumnName("policy_id").UseIdentityByDefaultColumn();
            e.Property(x => x.ProposalId).HasColumnName("proposal_id").IsRequired();
            e.Property(x => x.PaymentId).HasColumnName("payment_id").IsRequired();
            e.Property(x => x.PolicyNo).HasColumnName("policy_no").IsRequired();
            e.HasIndex(x => x.PolicyNo).IsUnique();
            e.Property(x => x.Make).HasColumnName("make");
            e.Property(x => x.Model).HasColumnName("model");
            e.Property(x => x.EngineNo).HasColumnName("engine_no").IsRequired();
            e.Property(x => x.ChassisNo).HasColumnName("chassis_no").IsRequired();
            e.Property(x => x.Premium).HasColumnName("premium").IsRequired();
            e.Property(x => x.Status).HasColumnName("status").IsRequired();
            e.Property(x => x.IssuedOn).HasColumnName("issued_on").IsRequired();
            e.HasOne(x => x.Proposal).WithMany().HasForeignKey(x => x.ProposalId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Payment).WithMany().HasForeignKey(x => x.PaymentId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PolicyCertificate>(e =>
        {
            e.ToTable("policy_certificate", Schema);
            e.HasKey(x => x.CertId);
            e.Property(x => x.CertId).HasColumnName("cert_id").UseIdentityByDefaultColumn();
            e.Property(x => x.PolicyId).HasColumnName("policy_id").IsRequired();
            e.Property(x => x.CertPath).HasColumnName("cert_path").IsRequired();
            e.Property(x => x.GeneratedOn).HasColumnName("generated_on").IsRequired();
            e.HasOne(x => x.Policy).WithMany().HasForeignKey(x => x.PolicyId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ReportLog>(e =>
        {
            e.ToTable("report_log", Schema);
            e.HasKey(x => x.ReportId);
            e.Property(x => x.ReportId).HasColumnName("report_id").UseIdentityByDefaultColumn();
            e.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
            e.Property(x => x.ReportType).HasColumnName("report_type").IsRequired();
            e.Property(x => x.FromDate).HasColumnName("from_date");
            e.Property(x => x.ToDate).HasColumnName("to_date");
            e.Property(x => x.GeneratedOn).HasColumnName("generated_on").IsRequired();
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AuditLog>(e =>
        {
            e.ToTable("audit_log", Schema);
            e.HasKey(x => x.AuditId);
            e.Property(x => x.AuditId).HasColumnName("audit_id").UseIdentityByDefaultColumn();
            e.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
            e.Property(x => x.EntityName).HasColumnName("entity_name").IsRequired();
            e.Property(x => x.Action).HasColumnName("action").IsRequired();
            e.Property(x => x.RefId).HasColumnName("ref_id");
            e.Property(x => x.Timestamp).HasColumnName("timestamp").IsRequired();
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
