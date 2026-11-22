# CusDec Index Scan — Facts for Proposed Index Evaluation

Read-only scan. Branch: `feature/cusdec-page`. Generated to support evaluation of
proposed indexes for the upcoming CusDec page. **Facts only, no recommendations.**

Scope of tables: `cus_decs`, `cus_dec_statuses`, `cus_dec_inspect_statuses`,
`cus_dec_containers`, `cus_dec_container_statuses`.

Note: the model snapshot (`CdnsDbContextModelSnapshot.cs`) is from before the
timestamp (`timestamp without time zone` → `timestamp with time zone`) change
currently in the working tree. Per instructions, the snapshot is used for
**indexes only**; its column types are not authoritative here. Working-tree
changes to `CusDecConfiguration.cs` did not add, remove, or alter any index.

---

## 1. Entities — full current content

### `Cdns.Domain/Entities/CusdecTables/CusDec.cs`

```csharp
namespace Cdns.Domain.Entities.CusdecTables;

public class CusDec
{
    public long CusDecId { get; set; }
    public string? CusDecNumber { get; set; }
    public string? DeclarantTinNumber { get; set; }
    public string? ConsigneeTinNumber { get; set; }

        // FKs to Company (nullable, Restrict) are configured in CusDecConfiguration
    public int? DeclarantCompanyId { get; set; }
    public int? ConsigneeCompanyId { get; set; }

    public decimal? InvoiceValue { get; set; }
    public int? NoOfItems { get; set; }
    public string? BlNumber { get; set; }
    public Guid GlobalIdentity { get; set; }
    public long CusDecExternalSystemNumber { get; set; }
    public DateTime LastSyncDateTime { get; set; }

    public long? DeclarantClaimedById { get; set; }
    public long? ConsigneeClaimedById { get; set; }
    public long? DeclarantDisclaimedById { get; set; }
    public long? ConsigneeDisclaimedById { get; set; }
    public DateTime? DeclarantClaimedDateTime { get; set; }
    public DateTime? ConsigneeClaimedDateTime { get; set; }
    public DateTime? DeclarantDisclaimedDateTime { get; set; }
    public DateTime? ConsigneeDisclaimedDateTime { get; set; }

    public bool IsReadyToProcess { get; set; }
    public long? ReadyToProcessMarkById { get; set; }
    public DateTime? ReadyToProcessMarkDateTime { get; set; }

    public DateTime? CusDecDate { get; set; }
    public string? Origin { get; set; }
    public string? OfficeCode { get; set; }
    public DateTime? ReleaseDate { get; set; }
    public DateTime? RegistrationDate { get; set; }
    public string? Exporter { get; set; }
    public string? Incoterm { get; set; }
    public string? Destination { get; set; }
    public string? Channel { get; set; }
    public int NoOfContainers { get; set; }
    public string? CusDecType { get; set; }

    public bool CanNotProcess { get; set; }
    public long? CanNotProcessById { get; set; }
    public DateTime? CanNotProcessMarkDateTime { get; set; }
    public string? CanNotProcessReason { get; set; }

    public bool CanProcess { get; set; }
    public long? CanProcessById { get; set; }
    public DateTime? CanProcessMarkDateTime { get; set; }

    public string? DeclarantCompanyName { get; set; }
    public string? ConsigneeCompanyName { get; set; }
    public string? SupplierCompanyName { get; set; }
}
```

### `Cdns.Domain/Entities/CusdecTables/CusDecStatus.cs`

```csharp
namespace Cdns.Domain.Entities.CusdecTables;

public class CusDecStatus
{
    public long CusDecStatusId { get; set; }
    public long? CusDecId { get; set; }
    public Guid? CusDecGlobalIdentity { get; set; }
    public string? Status { get; set; }
    public string? Description { get; set; }
    public DateTime? StatusDateTime { get; set; }
    public DateTime LastSyncDateTime { get; set; }
    public long ExternalSystemNumber { get; set; }
    public int CusDecStatusNumber { get; set; }
    public long CusDecExternalSystemNumber { get; set; }
    public string TableName { get; set; } = null!;

    // Navigation
    public CusDec? CusDec { get; set; }
}
```

### `Cdns.Domain/Entities/CusdecTables/CusDecInspectStatus.cs`

```csharp
namespace Cdns.Domain.Entities.CusdecTables;

public class CusDecInspectStatus
{
    public long CusDecInspectStatusId { get; set; }
    public long? CusDecId { get; set; }
    public Guid? CusDecGlobalIdentity { get; set; }
    public string Status { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime? StatusDateTime { get; set; }
    public DateTime LastSyncDateTime { get; set; }
    public long ExternalSystemNumber { get; set; }
    public int? CusDecStatusNumber { get; set; }
    public long CusDecExternalSystemNumber { get; set; }
    public string TableName { get; set; } = null!;

    // Navigation
    public CusDec? CusDec { get; set; }
}
```

### `Cdns.Domain/Entities/CusdecTables/CusDecContainer.cs`

```csharp
namespace Cdns.Domain.Entities.CusdecTables;

public class CusDecContainer
{
    public long CusDecContainerId { get; set; }
    public long? CusDecId { get; set; }
    public Guid? CusDecGlobalIdentity { get; set; }
    public Guid? GlobalIdentity { get; set; }
    public string? Container { get; set; }
    public string? Location { get; set; }
    public string? CurrentStatus { get; set; }
    public DateTime LastSyncDate { get; set; }
    public string? Size { get; set; }
    public string? Type { get; set; }
    public DateTime? ArrivalDate { get; set; }
    public string? Voyage { get; set; }
    public string? Vessel { get; set; }
    public long CusDecExternalSystemNumber { get; set; }
    public int ContainerKey { get; set; }
    public int ContainerPassNumber { get; set; }

    // Navigation
    public CusDec? CusDec { get; set; }
}
```

### `Cdns.Domain/Entities/CusdecTables/CusDecContainerStatus.cs`

```csharp
namespace Cdns.Domain.Entities.CusdecTables;

public class CusDecContainerStatus
{
    public long CusDecContainerStatusId { get; set; }
    public long? CusDecContainerId { get; set; }
    public string? Status { get; set; }
    public string? Description { get; set; }
    public DateTime? StatusDateTime { get; set; }
    public DateTime LastSyncDate { get; set; }
    public long? CusDecExternalSystemNumber { get; set; }
    public string? ContainerNumber { get; set; }
    public long ExternalSystemNumber { get; set; }
    public int ContainerStatusNumber { get; set; }

    // Navigation
    public CusDecContainer? CusDecContainer { get; set; }
}
```

---

## 2. Configurations — full current content

### `CusDecConfiguration.cs` (working-tree current, post-timestamp-change)

```csharp
using Cdns.Domain.Entities.CompanySubscriptionManagement;
using Cdns.Domain.Entities.CusdecTables;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cdns.Infrastructure.Persistence.Configurations.Cusdec;

public class CusDecConfiguration : IEntityTypeConfiguration<CusDec>
{
    public void Configure(EntityTypeBuilder<CusDec> builder)
    {

        builder.HasKey(x => x.CusDecId);
        builder.Property(x => x.CusDecId).ValueGeneratedOnAdd();

        builder.Property(x => x.CusDecNumber).HasMaxLength(200);
        builder.Property(x => x.DeclarantTinNumber).HasMaxLength(200);
        builder.Property(x => x.ConsigneeTinNumber).HasMaxLength(200);

        builder.Property(x => x.InvoiceValue).HasPrecision(18, 2);
        builder.Property(x => x.BlNumber).HasMaxLength(200);

        builder.Property(x => x.GlobalIdentity).IsRequired();
        builder.Property(x => x.CusDecExternalSystemNumber).IsRequired();

        builder.Property(x => x.LastSyncDateTime)
            .IsRequired()
            .HasDefaultValueSql("now()");

        builder.Property(x => x.IsReadyToProcess).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.CanNotProcess).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.CanProcess).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.NoOfContainers).IsRequired().HasDefaultValue(0);

        builder.Property(x => x.Origin).HasMaxLength(100);
        builder.Property(x => x.OfficeCode).HasMaxLength(20);
        builder.Property(x => x.Exporter).HasMaxLength(500);
        builder.Property(x => x.Incoterm).HasMaxLength(100);
        builder.Property(x => x.Destination).HasMaxLength(200);
        builder.Property(x => x.Channel).HasMaxLength(50);
        builder.Property(x => x.CusDecType).HasMaxLength(50);

        builder.Property(x => x.CanNotProcessReason).HasColumnType("text");

        builder.Property(x => x.DeclarantCompanyName).HasMaxLength(500);
        builder.Property(x => x.ConsigneeCompanyName).HasMaxLength(500);
        builder.Property(x => x.SupplierCompanyName).HasMaxLength(500);

        // ---- Foreign keys to Company ----
        // Nullable: the ASYCUDA sync inserts the CusDec first and the company link
        // is backfilled afterwards. Restrict: deleting a Company must never erase
        // or detach declaration history.
        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(x => x.DeclarantCompanyId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(x => x.ConsigneeCompanyId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.DeclarantCompanyId).HasDatabaseName("IX_CusDec_DeclarantCompanyId");
        builder.HasIndex(x => x.ConsigneeCompanyId).HasDatabaseName("IX_CusDec_ConsigneeCompanyId");

        builder.HasIndex(x => x.CusDecExternalSystemNumber).IsUnique();

        // The *ById columns (claim, disclaim, ready-to-process, can/can't process) are
        // plain bigint in legacy with no FK, so they stay that way for now.
    }
}
```

**Explicit index declarations in this file:**
- `HasIndex(DeclarantCompanyId)` → name `IX_CusDec_DeclarantCompanyId` (non-unique)
- `HasIndex(ConsigneeCompanyId)` → name `IX_CusDec_ConsigneeCompanyId` (non-unique)
- `HasIndex(CusDecExternalSystemNumber).IsUnique()` (unique)
- Two Company FKs (`DeclarantCompanyId`, `ConsigneeCompanyId`) — but their automatic FK index is **replaced** by the explicit `HasIndex` above (same columns).

### `CusDecStatusConfiguration.cs`

```csharp
using Cdns.Domain.Entities.CusdecTables;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cdns.Infrastructure.Persistence.Configurations.Cusdec;

public class CusDecStatusConfiguration
    : IEntityTypeConfiguration<CusDecStatus>
{
    public void Configure(EntityTypeBuilder<CusDecStatus> builder)
    {

        // Primary Key
        builder.HasKey(x => x.CusDecStatusId);

        builder.Property(x => x.CusDecStatusId)
            .ValueGeneratedOnAdd();

        // Properties
        builder.Property(x => x.Status)
            .HasMaxLength(500);

        builder.Property(x => x.Description);

        builder.Property(x => x.TableName)
            .HasMaxLength(100);

        builder.Property(x => x.LastSyncDateTime)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Foreign Key: CusDecId -> CusDec
        builder.HasOne(x => x.CusDec)
            .WithMany()
            .HasForeignKey(x => x.CusDecId)
            .HasPrincipalKey(x => x.CusDecId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.ExternalSystemNumber, x.CusDecStatusNumber, x.CusDecExternalSystemNumber, x.TableName })
        .IsUnique();
    }
}
```

**Explicit index declarations:**
- `HasIndex(ExternalSystemNumber, CusDecStatusNumber, CusDecExternalSystemNumber, TableName).IsUnique()`
- FK `CusDecId` → automatic FK index created (see §3).

### `CusDecInspectStatusConfiguration.cs`

```csharp
using Cdns.Domain.Entities.CusdecTables;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cdns.Infrastructure.Persistence.Configurations.Cusdec;

public class CusDecInspectStatusConfiguration
    : IEntityTypeConfiguration<CusDecInspectStatus>
{
    public void Configure(EntityTypeBuilder<CusDecInspectStatus> builder)
    {
        // Primary Key
        builder.HasKey(x => x.CusDecInspectStatusId);

        builder.Property(x => x.CusDecInspectStatusId)
            .ValueGeneratedOnAdd();

        // Properties
        builder.Property(x => x.Status)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.Description);

        builder.Property(x => x.TableName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.LastSyncDateTime)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Foreign Key: CusDecId -> CusDec
        builder.HasOne(x => x.CusDec)
            .WithMany()
            .HasForeignKey(x => x.CusDecId)
            .HasPrincipalKey(x => x.CusDecId)
            .OnDelete(DeleteBehavior.Restrict);

        // Natural key, matching SyncCusDecInspectStatus's own MERGE match condition
        builder.HasIndex(x => new { x.ExternalSystemNumber, x.CusDecExternalSystemNumber, x.TableName, x.Status })
            .IsUnique();

        builder.Property(x => x.ExternalSystemNumber).IsRequired();
        builder.Property(x => x.CusDecExternalSystemNumber).IsRequired();
    }
}
```

**Explicit index declarations:**
- `HasIndex(ExternalSystemNumber, CusDecExternalSystemNumber, TableName, Status).IsUnique()`
- FK `CusDecId` → automatic FK index created (see §3).

### `CusDecContainerConfiguration.cs`

```csharp
using Cdns.Domain.Entities.CusdecTables;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cdns.Infrastructure.Persistence.Configurations.Cusdec;

public class CusDecContainerConfiguration : IEntityTypeConfiguration<CusDecContainer>
{
    public void Configure(EntityTypeBuilder<CusDecContainer> builder)
    {

        builder.HasKey(x => x.CusDecContainerId);
        builder.Property(x => x.CusDecContainerId).ValueGeneratedOnAdd();

        builder.Property(x => x.Container).HasMaxLength(100);
        builder.Property(x => x.Location).HasMaxLength(100);
        builder.Property(x => x.CurrentStatus).HasMaxLength(100);
        builder.Property(x => x.Size).HasMaxLength(50);
        builder.Property(x => x.Type).HasMaxLength(50);
        builder.Property(x => x.Voyage).HasMaxLength(50);
        builder.Property(x => x.Vessel).HasMaxLength(50);

        builder.Property(x => x.LastSyncDate)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(x => x.ContainerPassNumber)
            .IsRequired()
            .HasDefaultValue(0);

        builder.HasOne(x => x.CusDec)
            .WithMany()
            .HasForeignKey(x => x.CusDecId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.CusDecExternalSystemNumber, x.ContainerKey }).IsUnique();

        builder.Property(x => x.CusDecExternalSystemNumber).IsRequired();
        
        builder.Property(x => x.ContainerKey).IsRequired();

    }
}
```

**Explicit index declarations:**
- `HasIndex(CusDecExternalSystemNumber, ContainerKey).IsUnique()`
- FK `CusDecId` → automatic FK index created (see §3).

### `CusDecContainerStatusConfiguration.cs`

```csharp
using Cdns.Domain.Entities.CusdecTables;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cdns.Infrastructure.Persistence.Configurations.Cusdec;

public class CusDecContainerStatusConfiguration : IEntityTypeConfiguration<CusDecContainerStatus>
{
    public void Configure(EntityTypeBuilder<CusDecContainerStatus> builder)
    {

        builder.HasKey(x => x.CusDecContainerStatusId);
        builder.Property(x => x.CusDecContainerStatusId).ValueGeneratedOnAdd();

        builder.Property(x => x.Status).HasMaxLength(200);
        builder.Property(x => x.Description); // NVARCHAR(MAX) -> text in Postgres
        builder.Property(x => x.ContainerNumber).HasMaxLength(100);

        builder.Property(x => x.LastSyncDate)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasOne(x => x.CusDecContainer)
            .WithMany()
            .HasForeignKey(x => x.CusDecContainerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.ExternalSystemNumber, x.ContainerStatusNumber }).IsUnique();

        builder.Property(x => x.ExternalSystemNumber).IsRequired();
        builder.Property(x => x.ContainerStatusNumber).IsRequired();
        
    }
}
```

**Explicit index declarations:**
- `HasIndex(ExternalSystemNumber, ContainerStatusNumber).IsUnique()`
- FK `CusDecContainerId` → automatic FK index created (see §3).

---

## 3. Current index set per table (from `CdnsDbContextModelSnapshot.cs`)

Columns are listed in index order. "Source" distinguishes an explicit `HasIndex`
from EF's automatic FK index. None of these indexes has a filter (no partial /
filtered indexes are defined on any of the five tables).

### `cus_decs`

| index name | columns (in order) | unique? | filter | source |
|---|---|---|---|---|
| `pk_cus_decs` | `cus_dec_id` | yes (PK) | none | primary key |
| `ix_cus_decs_cus_dec_external_system_number` | `cus_dec_external_system_number` | yes | none | explicit `HasIndex` |
| `IX_CusDec_DeclarantCompanyId` | `declarant_company_id` | no | none | explicit `HasIndex` (also serves the Declarant Company FK) |
| `IX_CusDec_ConsigneeCompanyId` | `consignee_company_id` | no | none | explicit `HasIndex` (also serves the Consignee Company FK) |

Note: both Company FKs exist, but no *separate* automatic FK index is emitted —
the explicit single-column `HasIndex` on each FK column covers it.

### `cus_dec_statuses`

| index name | columns (in order) | unique? | filter | source |
|---|---|---|---|---|
| `pk_cus_dec_statuses` | `cus_dec_status_id` | yes (PK) | none | primary key |
| `ix_cus_dec_statuses_cus_dec_id` | `cus_dec_id` | no | none | automatic FK index (FK `CusDecId` → `cus_decs`) |
| `ix_cus_dec_statuses_external_system_number_cus_dec_status_numb` | `external_system_number`, `cus_dec_status_number`, `cus_dec_external_system_number`, `table_name` | yes | none | explicit `HasIndex` |

### `cus_dec_inspect_statuses`

| index name | columns (in order) | unique? | filter | source |
|---|---|---|---|---|
| `pk_cus_dec_inspect_statuses` | `cus_dec_inspect_status_id` | yes (PK) | none | primary key |
| `ix_cus_dec_inspect_statuses_cus_dec_id` | `cus_dec_id` | no | none | automatic FK index (FK `CusDecId` → `cus_decs`) |
| `ix_cus_dec_inspect_statuses_external_system_number_cus_dec_ext` | `external_system_number`, `cus_dec_external_system_number`, `table_name`, `status` | yes | none | explicit `HasIndex` |

### `cus_dec_containers`

| index name | columns (in order) | unique? | filter | source |
|---|---|---|---|---|
| `pk_cus_dec_containers` | `cus_dec_container_id` | yes (PK) | none | primary key |
| `ix_cus_dec_containers_cus_dec_id` | `cus_dec_id` | no | none | automatic FK index (FK `CusDecId` → `cus_decs`) |
| `ix_cus_dec_containers_cus_dec_external_system_number_container` | `cus_dec_external_system_number`, `container_key` | yes | none | explicit `HasIndex` |

### `cus_dec_container_statuses`

| index name | columns (in order) | unique? | filter | source |
|---|---|---|---|---|
| `pk_cus_dec_container_statuses` | `cus_dec_container_status_id` | yes (PK) | none | primary key |
| `ix_cus_dec_container_statuses_cus_dec_container_id` | `cus_dec_container_id` | no | none | automatic FK index (FK `CusDecContainerId` → `cus_dec_containers`) |
| `ix_cus_dec_container_statuses_external_system_number_container` | `external_system_number`, `container_status_number` | yes | none | explicit `HasIndex` |

**Summary of what IS indexed on these tables today:**
- `cus_decs`: PK on `cus_dec_id`; unique on `cus_dec_external_system_number`;
  non-unique on `declarant_company_id`; non-unique on `consignee_company_id`.
- The TIN columns (`declarant_tin_number`, `consignee_tin_number`) are **NOT indexed**.
- `office_code`, `registration_date`, status/claim/disclaim columns, `cus_dec_type`,
  `can_process`/`can_not_process`/`is_ready_to_process`, and all `*_date_time`
  columns on `cus_decs` are **NOT indexed**.
- Each child table has one automatic index on its `cus_dec_id` / `cus_dec_container_id`
  FK, plus its sync natural-key unique index. `status_date_time` /
  `cus_dec_status_number` are NOT separately indexed.

---

## 4. Relationships (FKs and container-status linkage)

### Foreign keys (column → principal table.column)

| table | FK column | → principal table.column | delete behavior | constraint name |
|---|---|---|---|---|
| `cus_decs` | `declarant_company_id` | `companies.company_id` | Restrict | `fk_cus_decs_companies_declarant_company_id` |
| `cus_decs` | `consignee_company_id` | `companies.company_id` | Restrict | `fk_cus_decs_companies_consignee_company_id` |
| `cus_dec_statuses` | `cus_dec_id` | `cus_decs.cus_dec_id` | Restrict | `fk_cus_dec_statuses_cus_decs_cus_dec_id` |
| `cus_dec_inspect_statuses` | `cus_dec_id` | `cus_decs.cus_dec_id` | Restrict | `fk_cus_dec_inspect_statuses_cus_decs_cus_dec_id` |
| `cus_dec_containers` | `cus_dec_id` | `cus_decs.cus_dec_id` | Restrict | `fk_cus_dec_containers_cus_decs_cus_dec_id` |
| `cus_dec_container_statuses` | `cus_dec_container_id` | `cus_dec_containers.cus_dec_container_id` | Restrict | `fk_cus_dec_container_statuses_cus_dec_containers_cus_dec_conta` |

Both Company FK principal keys reference `companies.company_id` (the Company PK).
All CusDec-child FKs use `HasPrincipalKey(CusDec.CusDecId)` explicitly (status /
inspect-status configs) or default to the PK (container config).

### How a container status links to its container

- **Mapped (EF) relationship:** `CusDecContainerStatus.CusDecContainerId`
  (nullable `bigint`) is the FK → `CusDecContainer.CusDecContainerId`. This is the
  only enforced relationship between the two tables.
- **Additional denormalized sync-match columns present but NOT FKs:**
  - `CusDecContainerStatus.CusDecExternalSystemNumber` (nullable `bigint`) and
    `CusDecContainerStatus.ContainerNumber` (`varchar(100)`) carry the external
    system / container identity but are not configured as foreign keys.
  - `CusDecContainer` identity columns are `CusDecExternalSystemNumber` +
    `ContainerKey` (its unique natural key), and `ContainerPassNumber`.
  - `CusDecContainerStatus`'s own unique natural key is
    (`ExternalSystemNumber`, `ContainerStatusNumber`) — note `ExternalSystemNumber`
    here is the status row's own external number, distinct from
    `CusDecExternalSystemNumber`.
- **Observation:** there is no unique index or FK on
  `CusDecContainerStatus.ContainerNumber` or on its `CusDecExternalSystemNumber`,
  so matching a status to a container by external numbers (rather than by the
  mapped `CusDecContainerId`) is not backed by an index.

### CusDec ↔ status/container identity columns (relevant to joins)

- `cus_dec_statuses`, `cus_dec_inspect_statuses`, `cus_dec_containers` each carry
  both `CusDecId` (the real FK) and `CusDecExternalSystemNumber` /
  `CusDecGlobalIdentity` (denormalized sync-match columns, no FK on the latter two).
- Joining a status/container back to its CusDec via `CusDecId` is index-backed
  (automatic FK index). Joining via `CusDecExternalSystemNumber` is **not**
  index-backed on the child side (that column only appears inside the composite
  unique natural-key index, not as a leading column in every case — e.g. in
  `cus_dec_statuses` it is the 3rd column of the natural key, so not usable as a
  standalone probe).

---

## 5. Existing query code on these tables (outside sync and seed)

Three call sites read these tables outside the sync pipeline and seed data. None
of them is the CusDec page (which does not exist yet — see §6).

### 5a. `DashboardSupport.LoadVisibleCusDecsAsync` — the "visible CusDecs" query

File: `backend/src/Cdns.Infrastructure/Services/Dashboard/DashboardSupport.cs:33-70`

Full LINQ (lines 45-59):

```csharp
var rows = await db.CusDecs
    .Where(c =>
        (c.DeclarantTinNumber != null && declKeys.Contains(c.DeclarantTinNumber.Substring(0, 9))) ||
        (c.ConsigneeTinNumber != null && consKeys.Contains(c.ConsigneeTinNumber.Substring(0, 9))))
    .Select(c => new
    {
        c.CusDecId,
        c.OfficeCode,
        c.RegistrationDate,
        Statuses = db.CusDecStatuses
            .Where(s => s.CusDecId == c.CusDecId)
            .Select(s => new { s.Status, s.StatusDateTime, s.CusDecStatusNumber })
            .ToList(),
    })
    .ToListAsync(ct);
```

- **Tables read:** `cus_decs` (filter + projection), `cus_dec_statuses`
  (correlated subquery per CusDec).
- **Filter columns on `cus_decs`:** `declarant_tin_number` and
  `consignee_tin_number`, each via `Substring(0,9)` → SQL `substring(... from 1 for 9)`
  compared with `IN (@declKeys)` / `IN (@consKeys)`. **These TIN columns are not
  indexed, and the predicate is on `substring(...)`, so even if they were indexed
  a plain b-tree on the full column would not serve this `substring` predicate.**
- **Filter column on `cus_dec_statuses`:** `s.CusDecId == c.CusDecId`
  (correlated). `cus_dec_id` is index-backed (`ix_cus_dec_statuses_cus_dec_id`).
- **ORDER BY:** none in this query. (Status ordering by `Sequence`/`At` happens
  later in-memory in `DashboardCalculator`, not in SQL.)
- **Take / Skip / Count:** none. Loads **all** visible rows and all their statuses.
- Preceding setup (lines 36-42): `declKeys` / `consKeys` are built from the passed
  `scopes`; if both are empty the method returns `[]` before querying.
- A separate query in the same file loads status mappings
  (`DashboardSupport.cs:98-113`): `db.CusDecStatusMappings.Where(m => m.AsycudaStatus != null && m.CdnsStatus != null)` —
  that's the `cus_dec_status_mappings` lookup table, not one of the five tables in scope.

### How "visible CusDecs for a subscription" is decided

Defined in `DashboardSupport.LoadVisibleCusDecsAsync` + the `Scope` record +
the two dashboard services that build the scopes:

- **Match key:** first 9 characters of a TIN. `DashboardSupport.Key(tin)`
  (`DashboardSupport.cs:18-19`) returns `tin[..9]` (or the whole TIN if ≤ 9 chars).
- **Which column:** a subscription's `Scope` carries a `TinKey` and a `Side`
  (`SubscriptionType.Declarant` or `SubscriptionType.Consignee`). Declarant-side
  scopes match against `cus_decs.declarant_tin_number[..9]`; consignee-side scopes
  match against `cus_decs.consignee_tin_number[..9]` (`DashboardSupport.cs:36-48`).
- **A declaration is visible** when its declarant TIN first-9 is in the declarant
  scope set **OR** its consignee TIN first-9 is in the consignee scope set.
- **No claimed/disclaimed condition is applied here.** `LoadVisibleCusDecsAsync`
  does **not** filter on `DeclarantClaimedById`, `ConsigneeClaimedById`,
  `DeclarantDisclaimedById`, `ConsigneeDisclaimedById`, or any
  claim/disclaim/ready-to-process column. Visibility is purely TIN-first-9 +
  subscription side.
- **Scope origin (which subscriptions count):** only **active, unexpired** member
  subscriptions produce scopes. In both dashboard services, `activeSubs` =
  `MemberSubscriptionStatusEnum == Activate && ExpiryDate != null && ExpiryDate >= today`,
  then each active sub becomes a `Scope(Key(CompanyTin), (SubscriptionType)SubscriptionTypeEnum)`:
  - `CompanyDashboardService.cs:66-77`
  - `MemberDashboardService.cs:57-68`
  - Company dashboard scopes to all companies the master is mapped to
    (`CompanyMemberMappings` where `MemberId == masterId && !IsDeleted`);
    member dashboard scopes to `MemberSubscriptions` where `MemberId == memberId`.
- **Note (TIN-first-9 vs CompanyId):** the memory note
  `project_cusdec_module.md` records a design intent that the CusDec page should
  scope by `CompanyId` FK join (indexed), *not* TIN first-9. The **existing
  dashboard** code scopes by TIN first-9 (unindexed substring), as shown above.

### 5b. `CompanyDashboardService.LoadPaymentSummaryAsync`

File: `backend/src/Cdns.Infrastructure/Services/Dashboard/CompanyDashboardService.cs:97-119`

```csharp
var totals = await _db.CusDecPaymentSummaries
    .Where(p =>
        (p.DeclarantTinNumber != null && companyKeys.Contains(p.DeclarantTinNumber.Substring(0, 9))) ||
        (p.ConsigneeTinNumber != null && companyKeys.Contains(p.ConsigneeTinNumber.Substring(0, 9))))
    .GroupBy(_ => 1)
    .Select(g => new
    {
        Cif = g.Sum(x => x.TotalCif ?? 0m),
        Tax = g.Sum(x => x.TotalTaxPaid ?? 0m),
        Inspection = g.Sum(x => x.InspectionRecoveries ?? 0m),
    })
    .FirstOrDefaultAsync(ct);
```

- **Table read:** `cus_dec_payment_summaries` — **not one of the five tables in
  scope** (included here only because it is CusDec-family query code outside sync/seed).
- **Filter columns:** `declarant_tin_number`, `consignee_tin_number` via
  `Substring(0,9)` `IN (@companyKeys)`.
- **ORDER BY:** none. **GROUP BY:** constant (`GroupBy(_ => 1)`) → single aggregate row.
- **Take/Skip/Count:** `FirstOrDefaultAsync` (one row). No paging.

### 5c. `MasterActivationService.ClaimCusDecsForMaster`

File: `backend/src/Cdns.Infrastructure/Services/Auth/MasterActivationService.cs:135-161`

```csharp
var declarantCount = await _db.CusDecs
    .Where(c => c.DeclarantCompanyId == companyId
             && c.DeclarantClaimedById == null
             && c.DeclarantDisclaimedById == null)
    .ExecuteUpdateAsync(s => s
        .SetProperty(c => c.DeclarantClaimedById, memberId)
        .SetProperty(c => c.DeclarantClaimedDateTime, now),
    ct);

var consigneeCount = await _db.CusDecs
    .Where(c => c.ConsigneeCompanyId == companyId
             && c.ConsigneeClaimedById == null
             && c.ConsigneeDisclaimedById == null)
    .ExecuteUpdateAsync(s => s
        .SetProperty(c => c.ConsigneeClaimedById, memberId)
        .SetProperty(c => c.ConsigneeClaimedDateTime, now),
    ct);
```

- **Table written:** `cus_decs` (bulk `UPDATE ... WHERE`, no entities loaded).
- **Filter columns (declarant update):** `declarant_company_id == @companyId`
  (index-backed by `IX_CusDec_DeclarantCompanyId`), plus
  `declarant_claimed_by_id IS NULL` and `declarant_disclaimed_by_id IS NULL`
  (not indexed).
- **Filter columns (consignee update):** `consignee_company_id == @companyId`
  (index-backed by `IX_CusDec_ConsigneeCompanyId`), plus
  `consignee_claimed_by_id IS NULL` and `consignee_disclaimed_by_id IS NULL`
  (not indexed).
- **This is the one existing query that scopes by `CompanyId` (indexed FK column)**
  rather than TIN first-9.
- **ORDER BY / Take / Skip / Count:** none (it's an `ExecuteUpdateAsync`;
  `declarantCount` / `consigneeCount` are the affected-row counts returned by the
  bulk update, not a `.Count()` query).

---

## 6. CusDec page query code on `feature/cusdec-page`

**None.** There is no controller, endpoint, or service on the current branch that
queries `cus_decs` (or the status/container tables) for a CusDec page:

- No file under `backend/src/Cdns.Api` (Controllers or Endpoints) contains the
  string `cusdec` (case-insensitive). Existing controllers are
  `CompanyController`, `MasterController`, `MemberController`, `SignupController`,
  and `AuthEndpoints` — none references CusDec list/detail.
- No service under `backend/src/Cdns.Application` or
  `backend/src/Cdns.Infrastructure/Services` queries these tables for a page. The
  only `Cdns.Application` files mentioning "cusdec" are DTO/interface/dashboard
  files (`CusDecSyncRow.cs`, `DashboardCalculator.cs`, `DashboardModels.cs`,
  `IAsycudaSyncService.cs`) — none issues a page query.
- The only runtime readers of these tables are the three §5 sites (dashboard
  visibility, dashboard payment summary, master-activation claim) plus the sync
  pipeline (`AsycudaSyncService`, `CusDecSyncProcessor`) and seed
  (`MemberPortalSeedData`), all excluded per instructions.

Working-tree status at scan time (uncommitted): the files modified are the
timestamp/timezone change set (`DashboardCalculator.cs`, `AsycudaSyncService.cs`,
`CusDecConfiguration.cs`, `MasterActivationService.cs`, `CompanyDashboardService.cs`,
`MemberDashboardService.cs`, `CusDecSyncProcessor.cs`, `CusDecSyncProcessorTests.cs`)
plus two new files (`SriLankaTime.cs`, `SriLankaTimeTests.cs`). None adds CusDec
page query code.
```
