using System.Data;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.SqlClient;

const string OldConnStr = "Server=localhost\\SQLEXPRESS;Database=AslamDbNew;Trusted_Connection=True;TrustServerCertificate=True;";
const string NewConnStr = "Server=localhost\\SQLEXPRESS;Database=CampRegistrationDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

Console.WriteLine("=== Camp Registration Data Migrator ===");
Console.WriteLine($"From: AslamDbNew  To: CampRegistrationDb");
Console.WriteLine();

await using var oldConn = new SqlConnection(OldConnStr);
await oldConn.OpenAsync();
Console.WriteLine("Connected to old database.");

await using var newConn = new SqlConnection(NewConnStr);
await newConn.OpenAsync();
Console.WriteLine("Connected to new database.");
Console.WriteLine();

// --- Delegates to Sectors ---
Console.WriteLine("--- Delegates -> Sectors ---");
var delegateMap = new Dictionary<int, string>();
var sectorIdMap = new Dictionary<string, int>();

using (var cmd = new SqlCommand("SELECT Id, Name, Camp, coordinate, Area, munifactureTents, handmadeTents, bathrooms FROM Delegates ORDER BY Id", oldConn))
using (var reader = await cmd.ExecuteReaderAsync())
{
    while (await reader.ReadAsync())
    {
        var id = reader.GetInt32(0);
        var name = reader.GetString(1).Trim();
        delegateMap[id] = name;
        var camp = reader.IsDBNull(2) ? "" : reader.GetString(2).Trim();
        var coord = reader.IsDBNull(3) ? "" : reader.GetString(3).Trim();
        var area = reader.IsDBNull(4) ? 0 : reader.GetInt32(4);
        var manu = reader.IsDBNull(5) ? 0 : reader.GetInt32(5);
        var hand = reader.IsDBNull(6) ? 0 : reader.GetInt32(6);
        var bath = reader.IsDBNull(7) ? 0 : reader.GetInt32(7);

        var checkCmd = new SqlCommand("SELECT Id FROM Sectors WHERE Name = @name", newConn);
        checkCmd.Parameters.AddWithValue("@name", name);
        var existingId = await checkCmd.ExecuteScalarAsync();

        if (existingId != null)
        {
            sectorIdMap[name] = (int)existingId;
        }
        else
        {
            var insertSector = new SqlCommand(
                "INSERT INTO Sectors (Name, Camp, Coordinate, Area, ManufacturedTentsCount, HandmadeTentsCount, BathroomsCount) " +
                "OUTPUT INSERTED.Id VALUES (@n, @c, @coord, @a, @m, @h, @b)", newConn);
            insertSector.Parameters.AddWithValue("@n", name);
            insertSector.Parameters.AddWithValue("@c", camp);
            insertSector.Parameters.AddWithValue("@coord", coord);
            insertSector.Parameters.AddWithValue("@a", area.ToString());
            insertSector.Parameters.AddWithValue("@m", manu);
            insertSector.Parameters.AddWithValue("@h", hand);
            insertSector.Parameters.AddWithValue("@b", bath);
            var newId = (int)await insertSector.ExecuteScalarAsync();
            sectorIdMap[name] = newId;
            Console.WriteLine($"  + Sector: {name}");
        }
    }
}
Console.WriteLine($"  Sectors count: {sectorIdMap.Count}");

// Build Mandoob -> sector name map
var mandoobSectorMap = new Dictionary<string, string>();
var delegateNames = delegateMap.Values.ToHashSet(StringComparer.OrdinalIgnoreCase);

using (var cmd = new SqlCommand("SELECT DISTINCT LTRIM(RTRIM(ISNULL(Mandoob, ''))) AS Mandoob FROM refugees WHERE Mandoob IS NOT NULL AND LTRIM(RTRIM(Mandoob)) != ''", oldConn))
using (var reader = await cmd.ExecuteReaderAsync())
{
    while (await reader.ReadAsync())
    {
        var mandoob = reader.GetString(0).Trim();
        var match = delegateNames.FirstOrDefault(d => d.Contains(mandoob) || mandoob.Contains(d));
        mandoobSectorMap[mandoob] = match ?? mandoob;
    }
}

// Ensure default sector "A" exists
var checkA = new SqlCommand("SELECT Id FROM Sectors WHERE Name = 'A'", newConn);
var aId = await checkA.ExecuteScalarAsync();
if (aId != null)
    sectorIdMap["A"] = (int)aId;

// --- Load all refugees ---
Console.WriteLine("\n--- Refugees -> Families ---");
var refugeeRows = new List<RefugeeRow>();
using (var cmd = new SqlCommand(@"
    SELECT id, NationalId, FirstName, SecondName, ThirdName, FamilyName,
           ISNULL(Martial, ISNULL(MartialStatus, '')) AS MartialVal,
           BirthDate, Sickness, SicknessDetails, Disability, DisabilityDetails,
           Mobile, Job, Governate, Mandoob, Note,
           Pragenet, breastFeader, NeedElderlyDiaper, ChildLeadFamily,
           injury, injuryDate, Sex
    FROM refugees ORDER BY id", oldConn))
using (var reader = await cmd.ExecuteReaderAsync())
{
    while (await reader.ReadAsync())
    {
        refugeeRows.Add(new RefugeeRow
        {
            Id = reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
            NationalId = reader.IsDBNull(1) ? null : reader.GetInt32(1).ToString(),
            FirstName = reader.IsDBNull(2) ? "" : reader.GetString(2).Trim(),
            SecondName = reader.IsDBNull(3) ? "" : reader.GetString(3).Trim(),
            ThirdName = reader.IsDBNull(4) ? "" : reader.GetString(4).Trim(),
            FamilyName = reader.IsDBNull(5) ? "" : reader.GetString(5).Trim(),
            Martial = reader.IsDBNull(6) ? "" : reader.GetString(6).Trim(),
            BirthDate = reader.IsDBNull(7) ? (DateTime?)null : reader.GetDateTime(7),
            Sickness = reader.IsDBNull(8) ? "" : reader.GetString(8).Trim(),
            SicknessDetails = reader.IsDBNull(9) ? "" : reader.GetString(9).Trim(),
            Disability = reader.IsDBNull(10) ? "" : reader.GetString(10).Trim(),
            DisabilityDetails = reader.IsDBNull(11) ? "" : reader.GetString(11).Trim(),
            Mobile = reader.IsDBNull(12) ? (int?)null : reader.GetInt32(12),
            Job = reader.IsDBNull(13) ? "" : reader.GetString(13).Trim(),
            Governate = reader.IsDBNull(14) ? "" : reader.GetString(14).Trim(),
            Mandoob = reader.IsDBNull(15) ? "" : reader.GetString(15).Trim(),
            Note = reader.IsDBNull(16) ? "" : reader.GetString(16).Trim(),
            Pragenet = reader.IsDBNull(17) ? "" : reader.GetString(17).Trim(),
            BreastFeader = reader.IsDBNull(18) ? "" : reader.GetString(18).Trim(),
            NeedElderlyDiaper = reader.IsDBNull(19) ? "" : reader.GetString(19).Trim(),
            ChildLeadFamily = reader.IsDBNull(20) ? false : reader.GetBoolean(20),
            Injury = reader.IsDBNull(21) ? "" : reader.GetString(21).Trim(),
            InjuryDate = reader.IsDBNull(22) ? (DateTime?)null : reader.GetDateTime(22),
            Sex = reader.IsDBNull(23) ? "" : reader.GetString(23).Trim()
        });
    }
}
Console.WriteLine($"  Loaded {refugeeRows.Count} refugees.");

// Load existing IDs to skip duplicates
var existingIdNumbers = new HashSet<string>();
using (var cmd = new SqlCommand("SELECT IdNumber FROM Persons", newConn))
using (var reader = await cmd.ExecuteReaderAsync())
{
    while (await reader.ReadAsync())
        existingIdNumbers.Add(reader.IsDBNull(0) ? "" : reader.GetString(0));
}

int migrated = 0, skipped = 0, errors = 0;

foreach (var row in refugeeRows)
{
    SqlTransaction? transaction = null;
    try
    {
        transaction = newConn.BeginTransaction();

        if (string.IsNullOrEmpty(row.NationalId) || string.IsNullOrEmpty(row.FirstName))
        { skipped++; await transaction.RollbackAsync(); transaction = null; continue; }

        var idNumber = row.NationalId.PadLeft(9, '0');
        if (existingIdNumbers.Contains(idNumber))
        { skipped++; await transaction.RollbackAsync(); transaction = null; continue; }

        var maritalStatus = MapMartial(row.Martial);
        var healthStatus = MapHealthStatus(row.Sickness);
        var chronicDiseases = row.SicknessDetails;
        var disabilityTypes = MapDisabilityTypes(row.Disability, row.DisabilityDetails);
        var gender = MapGender(row.Sex);
        var sectorName = MapSector(row.Mandoob, mandoobSectorMap);

        bool? isPregnant = ParseBool(row.Pragenet);
        bool? isNursing = ParseBool(row.BreastFeader);
        bool hasInjury = !string.IsNullOrEmpty(row.Injury);
        bool needsDiapers = ParseBool(row.NeedElderlyDiaper) ?? false;

        var phone = row.Mobile.HasValue ? "0" + row.Mobile.Value.ToString() : "";
        if (phone.Length > 10) phone = phone[..10];
        if (phone == "0") phone = "";

        var recordId = GenerateRecordId();
        while (true)
        {
            var check = new SqlCommand("SELECT COUNT(1) FROM FamilyRegistrations WHERE RecordId = @rid", newConn, transaction);
            check.Parameters.AddWithValue("@rid", recordId);
            var exists = (int)await check.ExecuteScalarAsync();
            if (exists == 0) break;
            recordId = GenerateRecordId();
        }

        var passwordHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(idNumber)));

        // Insert Person (family head)
        var insertPerson = new SqlCommand(@"
            INSERT INTO Persons (FirstName, SecondName, ThirdName, LastName, IdNumber, DateOfBirth, Gender,
                OriginalGovernorate, MaritalStatus, EmploymentStatus, EducationLevel, HealthStatus,
                ChronicDiseases, DisabilityTypes, HasInjury, InjuryDate, InjuryDetails,
                IsPregnant, IsNursing, IsPrisoner, IsHouseDestroyed, BathroomStatus, MotherIdNumber, Nationality)
            OUTPUT INSERTED.Id
            VALUES (@fn, @sn, @tn, @ln, @idn, @dob, @g,
                @gov, @ms, @emp, @edu, @hs,
                @cd, @dt, @hi, @hid, @hidet,
                @ip, @in, @ipr, @ihd, @bs, @min, @nat)", newConn, transaction);

        insertPerson.Parameters.AddWithValue("@fn", row.FirstName);
        insertPerson.Parameters.AddWithValue("@sn", row.SecondName);
        insertPerson.Parameters.AddWithValue("@tn", row.ThirdName);
        insertPerson.Parameters.AddWithValue("@ln", row.FamilyName);
        insertPerson.Parameters.AddWithValue("@idn", idNumber);
        insertPerson.Parameters.AddWithValue("@dob", (object?)row.BirthDate ?? DBNull.Value);
        insertPerson.Parameters.AddWithValue("@g", gender);
        insertPerson.Parameters.AddWithValue("@gov", row.Governate);
        insertPerson.Parameters.AddWithValue("@ms", maritalStatus);
        insertPerson.Parameters.AddWithValue("@emp", row.Job);
        insertPerson.Parameters.AddWithValue("@edu", "");
        insertPerson.Parameters.AddWithValue("@hs", healthStatus);
        insertPerson.Parameters.AddWithValue("@cd", chronicDiseases);
        insertPerson.Parameters.AddWithValue("@dt", disabilityTypes);
        insertPerson.Parameters.AddWithValue("@hi", hasInjury);
        insertPerson.Parameters.AddWithValue("@hid", hasInjury && row.InjuryDate.HasValue ? (object)row.InjuryDate.Value : DBNull.Value);
        insertPerson.Parameters.AddWithValue("@hidet", hasInjury ? row.Injury : "");
        insertPerson.Parameters.AddWithValue("@ip", (object?)isPregnant ?? DBNull.Value);
        insertPerson.Parameters.AddWithValue("@in", (object?)isNursing ?? DBNull.Value);
        insertPerson.Parameters.AddWithValue("@ipr", false);
        insertPerson.Parameters.AddWithValue("@ihd", false);
        insertPerson.Parameters.AddWithValue("@bs", DBNull.Value);
        insertPerson.Parameters.AddWithValue("@min", DBNull.Value);
        insertPerson.Parameters.AddWithValue("@nat", "فلسطين");

        var headPersonId = (int)await insertPerson.ExecuteScalarAsync();
        existingIdNumbers.Add(idNumber);

        // Insert FamilyRegistration
        var insertFamily = new SqlCommand(@"
            INSERT INTO FamilyRegistrations (RecordId, RegistrationTimestamp, FamilyHeadId,
                IsChildHeaded, IsFemaleHeaded, IsHusbandAbroad, SupportsOutsidePerson,
                LivesInTent, HasBathroom, NeedsDiapers, DiaperDetails, HasMultipleFamiliesInTent,
                Sector, PhoneNumber, Wallet, WalletType,
                PasswordHash, StatusNotes, ApprovalStatus, ApprovedAt, IsDeleted)
            OUTPUT INSERTED.Id
            VALUES (@rid, @ts, @fhid,
                @ich, @ifh, @iha, @sop,
                @lit, @hb, @nd, @dd, @hmf,
                @sec, @ph, @w, @wt,
                @pw, @sn, @as, @aa, @isd)", newConn, transaction);

        insertFamily.Parameters.AddWithValue("@rid", recordId);
        insertFamily.Parameters.AddWithValue("@ts", DateTime.UtcNow);
        insertFamily.Parameters.AddWithValue("@fhid", headPersonId);
        insertFamily.Parameters.AddWithValue("@ich", row.ChildLeadFamily);
        insertFamily.Parameters.AddWithValue("@ifh", false);
        insertFamily.Parameters.AddWithValue("@iha", false);
        insertFamily.Parameters.AddWithValue("@sop", false);
        insertFamily.Parameters.AddWithValue("@lit", false);
        insertFamily.Parameters.AddWithValue("@hb", false);
        insertFamily.Parameters.AddWithValue("@nd", needsDiapers);
        insertFamily.Parameters.AddWithValue("@dd", needsDiapers ? row.NeedElderlyDiaper : "");
        insertFamily.Parameters.AddWithValue("@hmf", false);
        insertFamily.Parameters.AddWithValue("@sec", sectorName);
        insertFamily.Parameters.AddWithValue("@ph", phone);
        insertFamily.Parameters.AddWithValue("@w", DBNull.Value);
        insertFamily.Parameters.AddWithValue("@wt", DBNull.Value);
        insertFamily.Parameters.AddWithValue("@pw", passwordHash);
        insertFamily.Parameters.AddWithValue("@sn", row.Note);
        insertFamily.Parameters.AddWithValue("@as", 1); // Approved
        insertFamily.Parameters.AddWithValue("@aa", DateTime.UtcNow);
        insertFamily.Parameters.AddWithValue("@isd", false);

        var registrationId = (int)await insertFamily.ExecuteScalarAsync();

        // Migrate sons
        await MigrateMembersAsync(oldConn, newConn, transaction, registrationId, row.NationalId!, idNumber, existingIdNumbers, "sons");

        // Migrate wives
        await MigrateWivesAsync(oldConn, newConn, transaction, registrationId, row.NationalId!, idNumber, existingIdNumbers);

        await transaction.CommitAsync();
        transaction = null;
        migrated++;
        if (migrated % 50 == 0)
            Console.WriteLine($"  Progress: {migrated}/{refugeeRows.Count} families...");
    }
    catch (Exception ex)
    {
        if (transaction != null) { try { await transaction.RollbackAsync(); } catch { } }
        errors++;
        Console.WriteLine($"  ERROR refugee {row.Id} ({row.FirstName} {row.FamilyName}): {ex.Message}");
    }
}

// Count results
using (var c1 = new SqlCommand("SELECT COUNT(1) FROM FamilyRegistrations", newConn))
using (var c2 = new SqlCommand("SELECT COUNT(1) FROM Persons", newConn))
using (var c3 = new SqlCommand("SELECT COUNT(1) FROM FamilyMembers", newConn))
{
    Console.WriteLine($"\n=== Migration Complete ===");
    Console.WriteLine($"  Families migrated: {migrated}");
    Console.WriteLine($"  Skipped (no ID/duplicate): {skipped}");
    Console.WriteLine($"  Errors: {errors}");
    Console.WriteLine($"  Total FamilyRegistrations: {await c1.ExecuteScalarAsync()}");
    Console.WriteLine($"  Total Persons: {await c2.ExecuteScalarAsync()}");
    Console.WriteLine($"  Total FamilyMembers: {await c3.ExecuteScalarAsync()}");
}

// ─── Helpers ──────────────────────────────────

static string MapMartial(string m)
{
    if (string.IsNullOrEmpty(m)) return "أعزب";
    if (m.Contains("متزوج")) return "متزوج";
    if (m.Contains("أعزب") || m.Contains("عازب")) return "أعزب";
    if (m.Contains("مطلق")) return "مطلق";
    if (m.Contains("أرمل") || m.Contains("ارمل")) return "أرمل";
    if (m.Contains("خطيب") || m.Contains("خاطب")) return "خاطب";
    return "أعزب";
}

static string MapGender(string s)
{
    if (string.IsNullOrEmpty(s)) return "ذكر";
    if (s.Contains("أنثى") || s.Contains("انثى") || s is "2" or "F" or "f") return "أنثى";
    return "ذكر";
}

static string MapSector(string mandoob, Dictionary<string, string> map)
{
    if (string.IsNullOrWhiteSpace(mandoob)) return "A";
    return map.TryGetValue(mandoob.Trim(), out var s) ? s : mandoob.Trim();
}

static bool? ParseBool(string v) =>
    string.IsNullOrEmpty(v) ? null : v.Contains("نعم") || v.Contains("yes");

static string MapHealthStatus(string sickness)
{
    // Old DB uses "مرض" (sick) vs "سليم" (healthy) explicitly
    if (string.IsNullOrWhiteSpace(sickness)) return "سليم";
    return sickness.Trim() == "مرض" ? "مريض" : "سليم";
}

static string MapDisabilityTypes(string disability, string details)
{
    // Old DB uses "معاق" (disabled) vs "غير معاق" (not disabled)
    if (string.IsNullOrWhiteSpace(disability) || disability.Trim() != "معاق")
        return string.IsNullOrWhiteSpace(details) ? "" : details;
    if (string.IsNullOrWhiteSpace(details)) return disability.Trim();
    return disability.Trim() + " - " + details.Trim();
}

static string GenerateRecordId()
{
    const string chars = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ";
    return new string(Enumerable.Range(0, 8).Select(_ => chars[Random.Shared.Next(chars.Length)]).ToArray());
}

static async Task MigrateMembersAsync(SqlConnection oldConn, SqlConnection newConn, SqlTransaction tx,
    int regId, string parentNationalId, string headIdNumber, HashSet<string> existingIds, string table)
{
    var parentId = int.Parse(parentNationalId);
    var sql = $@"SELECT NationalId, FirstName, SecondName, ThirdName, FamilyName, martailState,
                        Sickness, SicknessDetails, Disability, DisiabilityDetails,
                        BirthDate, sex, RelationShip, Notes
                 FROM {table}
                 WHERE ParentNationalId = @pid
                 ORDER BY id";

    using var cmd = new SqlCommand(sql, oldConn);
    cmd.Parameters.AddWithValue("@pid", parentId);
    using var reader = await cmd.ExecuteReaderAsync();

    while (await reader.ReadAsync())
    {
        var memNatId = reader.IsDBNull(0) ? null : reader.GetInt32(0).ToString();
        var fn = reader.IsDBNull(1) ? "" : reader.GetString(1).Trim();
        var sn = reader.IsDBNull(2) ? "" : reader.GetString(2).Trim();
        var tn = reader.IsDBNull(3) ? "" : reader.GetString(3).Trim();
        var ln = reader.IsDBNull(4) ? "" : reader.GetString(4).Trim();
        var ms = reader.IsDBNull(5) ? "" : reader.GetString(5).Trim();
        var sick = reader.IsDBNull(6) ? "" : reader.GetString(6).Trim();
        var sickDet = reader.IsDBNull(7) ? "" : reader.GetString(7).Trim();
        var dis = reader.IsDBNull(8) ? "" : reader.GetString(8).Trim();
        var disDet = reader.IsDBNull(9) ? "" : reader.GetString(9).Trim();
        var bd = reader.IsDBNull(10) ? (DateTime?)null : reader.GetDateTime(10);
        var sex = reader.IsDBNull(11) ? "" : reader.GetString(11).Trim();
        var rel = reader.IsDBNull(12) ? "" : reader.GetString(12).Trim();

        if (string.IsNullOrEmpty(fn) || string.IsNullOrEmpty(memNatId)) continue;

        var idn = memNatId.PadLeft(9, '0');
        if (existingIds.Contains(idn) || idn == headIdNumber) continue;

        var h = MapHealthStatus(sick);
        var dt = MapDisabilityTypes(dis, disDet);

        // Insert member Person
        var ip = new SqlCommand(@"
            INSERT INTO Persons (FirstName, SecondName, ThirdName, LastName, IdNumber, DateOfBirth, Gender,
                OriginalGovernorate, MaritalStatus, EmploymentStatus, EducationLevel, HealthStatus,
                ChronicDiseases, DisabilityTypes, HasInjury, InjuryDate, InjuryDetails,
                IsPregnant, IsNursing, IsPrisoner, IsHouseDestroyed, BathroomStatus, MotherIdNumber, Nationality)
            OUTPUT INSERTED.Id
            VALUES (@fn, @sn, @tn, @ln, @idn, @dob, @g,
                @gov, @ms, @emp, @edu, @hs,
                @cd, @dt, @hi, @hid, @hidet,
                @ip, @in, @ipr, @ihd, @bs, @min, @nat)", newConn, tx);

        ip.Parameters.AddWithValue("@fn", fn);
        ip.Parameters.AddWithValue("@sn", sn);
        ip.Parameters.AddWithValue("@tn", tn);
        ip.Parameters.AddWithValue("@ln", string.IsNullOrEmpty(ln) ? fn : ln);
        ip.Parameters.AddWithValue("@idn", idn);
        ip.Parameters.AddWithValue("@dob", (object?)bd ?? DBNull.Value);
        ip.Parameters.AddWithValue("@g", MapGender(sex));
        ip.Parameters.AddWithValue("@gov", "");
        ip.Parameters.AddWithValue("@ms", MapMartial(ms));
        ip.Parameters.AddWithValue("@emp", "");
        ip.Parameters.AddWithValue("@edu", "");
        ip.Parameters.AddWithValue("@hs", h);
        ip.Parameters.AddWithValue("@cd", sickDet);
        ip.Parameters.AddWithValue("@dt", dt);
        ip.Parameters.AddWithValue("@hi", false);
        ip.Parameters.AddWithValue("@hid", DBNull.Value);
        ip.Parameters.AddWithValue("@hidet", "");
        ip.Parameters.AddWithValue("@ip", DBNull.Value);
        ip.Parameters.AddWithValue("@in", DBNull.Value);
        ip.Parameters.AddWithValue("@ipr", false);
        ip.Parameters.AddWithValue("@ihd", false);
        ip.Parameters.AddWithValue("@bs", DBNull.Value);
        ip.Parameters.AddWithValue("@min", DBNull.Value);
        ip.Parameters.AddWithValue("@nat", "فلسطين");

        var personId = (int)await ip.ExecuteScalarAsync();
        existingIds.Add(idn);

        // Insert FamilyMember
        var ifm = new SqlCommand(
            "INSERT INTO FamilyMembers (RegistrationId, PersonId, RelationshipToHead) VALUES (@rid, @pid, @rel)",
            newConn, tx);
        ifm.Parameters.AddWithValue("@rid", regId);
        ifm.Parameters.AddWithValue("@pid", personId);
        ifm.Parameters.AddWithValue("@rel", string.IsNullOrEmpty(rel) ? "ابن/ابنة" : rel);
        await ifm.ExecuteNonQueryAsync();
    }
}

static async Task MigrateWivesAsync(SqlConnection oldConn, SqlConnection newConn, SqlTransaction tx,
    int regId, string parentNationalId, string headIdNumber, HashSet<string> existingIds)
{
    var parentId = int.Parse(parentNationalId);
    using var cmd = new SqlCommand(@"
        SELECT NationalId, FirstName, SecondName, ThirdName, FamilyName,
               Sickness, SicknessDetails, Disability, DisiabiltyDetails,
               Job, BirthDate, Pregenent, BreastFeeder, NeedElderlyDiaper, Note
        FROM wivies
        WHERE ParentNationalId = @pid
        ORDER BY id", oldConn);
    cmd.Parameters.AddWithValue("@pid", parentId);

    using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        var memNatId = reader.IsDBNull(0) ? null : reader.GetInt32(0).ToString();
        var fn = reader.IsDBNull(1) ? "" : reader.GetString(1).Trim();
        var sn = reader.IsDBNull(2) ? "" : reader.GetString(2).Trim();
        var tn = reader.IsDBNull(3) ? "" : reader.GetString(3).Trim();
        var ln = reader.IsDBNull(4) ? "" : reader.GetString(4).Trim();
        var sick = reader.IsDBNull(5) ? "" : reader.GetString(5).Trim();
        var sickDet = reader.IsDBNull(6) ? "" : reader.GetString(6).Trim();
        var dis = reader.IsDBNull(7) ? "" : reader.GetString(7).Trim();
        var disDet = reader.IsDBNull(8) ? "" : reader.GetString(8).Trim();
        var job = reader.IsDBNull(9) ? "" : reader.GetString(9).Trim();
        var bd = reader.IsDBNull(10) ? (DateTime?)null : reader.GetDateTime(10);
        var preg = reader.IsDBNull(11) ? "" : reader.GetString(11).Trim();
        var breast = reader.IsDBNull(12) ? "" : reader.GetString(12).Trim();

        if (string.IsNullOrEmpty(fn)) continue;

        var idn = string.IsNullOrEmpty(memNatId)
            ? $"9{parentNationalId}{regId % 1000:000}"
            : memNatId.PadLeft(9, '0');

        if (existingIds.Contains(idn) || idn == headIdNumber) continue;

        var h = MapHealthStatus(sick);
        var dt = MapDisabilityTypes(dis, disDet);

        var ip = new SqlCommand(@"
            INSERT INTO Persons (FirstName, SecondName, ThirdName, LastName, IdNumber, DateOfBirth, Gender,
                OriginalGovernorate, MaritalStatus, EmploymentStatus, EducationLevel, HealthStatus,
                ChronicDiseases, DisabilityTypes, HasInjury, InjuryDate, InjuryDetails,
                IsPregnant, IsNursing, IsPrisoner, IsHouseDestroyed, BathroomStatus, MotherIdNumber, Nationality)
            OUTPUT INSERTED.Id
            VALUES (@fn, @sn, @tn, @ln, @idn, @dob, @g,
                @gov, @ms, @emp, @edu, @hs,
                @cd, @dt, @hi, @hid, @hidet,
                @ip, @in, @ipr, @ihd, @bs, @min, @nat)", newConn, tx);

        ip.Parameters.AddWithValue("@fn", fn);
        ip.Parameters.AddWithValue("@sn", sn);
        ip.Parameters.AddWithValue("@tn", tn);
        ip.Parameters.AddWithValue("@ln", string.IsNullOrEmpty(ln) ? fn : ln);
        ip.Parameters.AddWithValue("@idn", idn);
        ip.Parameters.AddWithValue("@dob", (object?)bd ?? DBNull.Value);
        ip.Parameters.AddWithValue("@g", "أنثى");
        ip.Parameters.AddWithValue("@gov", "");
        ip.Parameters.AddWithValue("@ms", "متزوج");
        ip.Parameters.AddWithValue("@emp", job);
        ip.Parameters.AddWithValue("@edu", "");
        ip.Parameters.AddWithValue("@hs", h);
        ip.Parameters.AddWithValue("@cd", sickDet);
        ip.Parameters.AddWithValue("@dt", dt);
        ip.Parameters.AddWithValue("@hi", false);
        ip.Parameters.AddWithValue("@hid", DBNull.Value);
        ip.Parameters.AddWithValue("@hidet", "");
        ip.Parameters.AddWithValue("@ip", ParseBool(preg) ?? (object)DBNull.Value);
        ip.Parameters.AddWithValue("@in", ParseBool(breast) ?? (object)DBNull.Value);
        ip.Parameters.AddWithValue("@ipr", false);
        ip.Parameters.AddWithValue("@ihd", false);
        ip.Parameters.AddWithValue("@bs", DBNull.Value);
        ip.Parameters.AddWithValue("@min", DBNull.Value);
        ip.Parameters.AddWithValue("@nat", "فلسطين");

        var personId = (int)await ip.ExecuteScalarAsync();
        existingIds.Add(idn);

        var ifm = new SqlCommand(
            "INSERT INTO FamilyMembers (RegistrationId, PersonId, RelationshipToHead) VALUES (@rid, @pid, @rel)",
            newConn, tx);
        ifm.Parameters.AddWithValue("@rid", regId);
        ifm.Parameters.AddWithValue("@pid", personId);
        ifm.Parameters.AddWithValue("@rel", "زوجة");
        await ifm.ExecuteNonQueryAsync();
    }
}

public class RefugeeRow
{
    public int Id { get; set; }
    public string? NationalId { get; set; }
    public string FirstName { get; set; } = "";
    public string SecondName { get; set; } = "";
    public string ThirdName { get; set; } = "";
    public string FamilyName { get; set; } = "";
    public string Martial { get; set; } = "";
    public DateTime? BirthDate { get; set; }
    public string Sickness { get; set; } = "";
    public string SicknessDetails { get; set; } = "";
    public string Disability { get; set; } = "";
    public string DisabilityDetails { get; set; } = "";
    public int? Mobile { get; set; }
    public string Job { get; set; } = "";
    public string Governate { get; set; } = "";
    public string Mandoob { get; set; } = "";
    public string Note { get; set; } = "";
    public string Pragenet { get; set; } = "";
    public string BreastFeader { get; set; } = "";
    public string NeedElderlyDiaper { get; set; } = "";
    public bool ChildLeadFamily { get; set; }
    public string Injury { get; set; } = "";
    public DateTime? InjuryDate { get; set; }
    public string Sex { get; set; } = "";
}
