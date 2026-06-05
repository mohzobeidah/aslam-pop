using System.Globalization;
using CampRegistrationApp.Data;
using CampRegistrationApp.Models;
using CampRegistrationApp.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace CampRegistrationApp.Services;

public static class RegistrationChangeTracker
{
    public sealed class Snapshot
    {
        public Dictionary<string, object?> Head { get; set; } = new();
        public Dictionary<string, object?> Registration { get; set; } = new();
        public Dictionary<string, MemberSnap> Members { get; set; } = new();
        public List<int> Desires { get; set; } = new();
    }

    public sealed class MemberSnap
    {
        public Dictionary<string, object?> Fields { get; set; } = new();
    }

    public sealed class Diff
    {
        public Dictionary<string, Dictionary<string, FieldChange>> Head { get; } = new();
        public Dictionary<string, Dictionary<string, FieldChange>> Registration { get; } = new();
        public List<Dictionary<string, object?>> MembersAdded { get; } = new();
        public List<Dictionary<string, object?>> MembersRemoved { get; } = new();
        public Dictionary<string, Dictionary<string, FieldChange>> MembersModified { get; } = new();
        public List<int>? DesiresOld { get; set; }
        public List<int>? DesiresNew { get; set; }

        public bool IsEmpty =>
            Head.Count == 0
            && Registration.Count == 0
            && MembersAdded.Count == 0
            && MembersRemoved.Count == 0
            && MembersModified.Count == 0
            && DesiresOld == null;
    }

    public sealed class FieldChange
    {
        public object? Old { get; set; }
        public object? New { get; set; }
    }

    private static readonly string[] HeadFields = new[]
    {
        "FirstName", "SecondName", "ThirdName", "LastName", "IdNumber",
        "DateOfBirth", "Gender", "OriginalGovernorate", "MaritalStatus",
        "EmploymentStatus", "EducationLevel", "HealthStatus",
        "ChronicDiseases", "DisabilityTypes", "HasInjury", "InjuryDate",
        "InjuryDetails", "IsHouseDestroyed", "IsPrisoner", "IsPregnant",
        "PregnancyMonth", "IsNursing", "NursingInfantName", "NursingInfantDOB",
        "NursingInfantID", "MotherIdNumber", "BathroomStatus"
    };

    private static readonly string[] RegistrationFields = new[]
    {
        "SectorId", "PhoneNumber", "Wallet", "WalletType",
        "IsChildHeaded", "ChildHeadedDetails", "IsFemaleHeaded",
        "FemaleHeadedDetails", "SupportsOutsidePerson", "OutsidePersonName",
        "OutsidePersonRelation", "LivesInTent", "TentType", "OtherTentType",
        "HasBathroom", "BathroomType", "NeedsDiapers", "DiaperDetails",
        "HasMultipleFamiliesInTent", "AdditionalFamiliesCount",
        "StatusNotes", "IsHusbandAbroad"
    };

    private static readonly string[] MemberFields = new[]
    {
        "FirstName", "SecondName", "ThirdName", "LastName", "IdNumber",
        "DateOfBirth", "Gender", "OriginalGovernorate", "MaritalStatus",
        "EmploymentStatus", "EducationLevel", "HealthStatus",
        "ChronicDiseases", "DisabilityTypes", "HasInjury", "InjuryDate",
        "InjuryDetails", "IsPrisoner", "IsHusbandPrisoner",
        "IsPregnant", "PregnancyMonth", "IsNursing", "NursingInfantName",
        "NursingInfantDOB", "NursingInfantID", "MotherIdNumber",
        "BathroomStatus", "RelationshipToHead"
    };

    public static async Task<Snapshot> CaptureAsync(FamilyRegistration registration)
    {
        var snap = new Snapshot();

        var head = registration.FamilyHead;
        foreach (var f in HeadFields)
            snap.Head[f] = ReadField(head, f);

        foreach (var f in RegistrationFields)
            snap.Registration[f] = ReadField(registration, f);

        foreach (var m in registration.Members)
        {
            var id = m.Person.IdNumber;
            if (string.IsNullOrEmpty(id)) continue;
            var memberSnap = new MemberSnap();
            foreach (var f in MemberFields)
                memberSnap.Fields[f] = ReadField(m.Person, f);
            memberSnap.Fields["RelationshipToHead"] = m.RelationshipToHead;
            snap.Members[id] = memberSnap;
        }

        snap.Desires = registration.FamilyDesires
            .OrderBy(d => d.Order)
            .Select(d => d.DesireId)
            .ToList();

        await Task.CompletedTask;
        return snap;
    }

    public static async Task<Diff> BuildDiffAsync(
        ApplicationDbContext context,
        Snapshot before,
        RegistrationViewModel after)
    {
        var diff = new Diff();

        var newHead = new Dictionary<string, object?>();
        foreach (var f in HeadFields)
            newHead[f] = ReadPersonFieldFromViewModel(after.Head, f);

        foreach (var f in HeadFields)
        {
            var oldV = Normalize(before.Head.TryGetValue(f, out var v) ? v : null);
            var newV = Normalize(newHead[f]);
            if (!Equals(oldV, newV))
            {
                if (!diff.Head.TryGetValue("Changes", out var bucket))
                {
                    bucket = new Dictionary<string, FieldChange>();
                    diff.Head["Changes"] = bucket;
                }
                bucket[f] = new FieldChange { Old = oldV, New = newV };
            }
        }

        var sectorMap = await context.Sectors
            .AsNoTracking()
            .ToDictionaryAsync(s => s.Id, s => s.Name);

        var oldSector = before.Registration.TryGetValue("SectorId", out var os) ? Convert.ToInt32(os ?? 0) : 0;
        var newSector = after.SectorId ?? 0;
        var oldSectorName = sectorMap.TryGetValue(oldSector, out var osn) ? osn : oldSector.ToString();
        var newSectorName = sectorMap.TryGetValue(newSector, out var nsn) ? nsn : newSector.ToString();

        for (int i = 0; i < RegistrationFields.Length; i++)
        {
            var f = RegistrationFields[i];
            var oldV = Normalize(before.Registration.TryGetValue(f, out var v) ? v : null);
            var newV = Normalize(ReadFieldFromViewModel(after, f));

            if (f == "SectorId")
            {
                if (oldSector != newSector)
                {
                    if (!diff.Registration.TryGetValue("Changes", out var bucket))
                    {
                        bucket = new Dictionary<string, FieldChange>();
                        diff.Registration["Changes"] = bucket;
                    }
                    bucket["SectorId"] = new FieldChange { Old = oldSectorName, New = newSectorName };
                }
                continue;
            }

            if (!Equals(oldV, newV))
            {
                if (!diff.Registration.TryGetValue("Changes", out var bucket))
                {
                    bucket = new Dictionary<string, FieldChange>();
                    diff.Registration["Changes"] = bucket;
                }
                bucket[f] = new FieldChange { Old = oldV, New = newV };
            }
        }

        var newMembers = new Dictionary<string, MemberSnap>();
        foreach (var m in after.Members)
        {
            if (string.IsNullOrEmpty(m.IdNumber)) continue;
            var memberSnap = new MemberSnap();
            foreach (var f in MemberFields)
                memberSnap.Fields[f] = ReadMemberFieldFromViewModel(m, f);
            newMembers[m.IdNumber] = memberSnap;
        }

        foreach (var kv in newMembers)
        {
            if (!before.Members.ContainsKey(kv.Key))
            {
                diff.MembersAdded.Add(new Dictionary<string, object?>
                {
                    ["FirstName"] = kv.Value.Fields.GetValueOrDefault("FirstName"),
                    ["LastName"] = kv.Value.Fields.GetValueOrDefault("LastName"),
                    ["IdNumber"] = kv.Key,
                    ["RelationshipToHead"] = kv.Value.Fields.GetValueOrDefault("RelationshipToHead")
                });
            }
        }

        foreach (var kv in before.Members)
        {
            if (!newMembers.ContainsKey(kv.Key))
            {
                diff.MembersRemoved.Add(new Dictionary<string, object?>
                {
                    ["FirstName"] = kv.Value.Fields.GetValueOrDefault("FirstName"),
                    ["LastName"] = kv.Value.Fields.GetValueOrDefault("LastName"),
                    ["IdNumber"] = kv.Key,
                    ["RelationshipToHead"] = kv.Value.Fields.GetValueOrDefault("RelationshipToHead")
                });
            }
        }

        foreach (var kv in newMembers)
        {
            if (!before.Members.TryGetValue(kv.Key, out var oldMemberSnap)) continue;
            var changes = new Dictionary<string, FieldChange>();
            foreach (var f in MemberFields)
            {
                var oldV = Normalize(oldMemberSnap.Fields.TryGetValue(f, out var v) ? v : null);
                var newV = Normalize(kv.Value.Fields.TryGetValue(f, out var nv) ? nv : null);
                if (!Equals(oldV, newV))
                    changes[f] = new FieldChange { Old = oldV, New = newV };
            }
            if (changes.Count > 0)
                diff.MembersModified[kv.Key] = changes;
        }

        var oldDesires = before.Desires;
        var newDesires = (after.DesireIds ?? new List<int>()).Where(id => id > 0).ToList();
        if (!oldDesires.SequenceEqual(newDesires))
        {
            diff.DesiresOld = oldDesires;
            diff.DesiresNew = newDesires;
        }

        return diff;
    }

    public static object? ToAuditPayload(Diff diff, string action, string headName, string? sectorName, string? recordId)
    {
        var meta = new Dictionary<string, object?>
        {
            ["action"] = action,
            ["headName"] = headName,
            ["sector"] = sectorName,
            ["recordId"] = recordId,
            ["changedFieldCount"] = CountChangedFields(diff)
        };

        var result = new Dictionary<string, object?> { ["Meta"] = meta };

        if (diff.Head.Count > 0) result["head"] = diff.Head;
        if (diff.Registration.Count > 0) result["registration"] = diff.Registration;
        if (diff.MembersAdded.Count > 0) result["membersAdded"] = diff.MembersAdded;
        if (diff.MembersRemoved.Count > 0) result["membersRemoved"] = diff.MembersRemoved;
        if (diff.MembersModified.Count > 0) result["membersModified"] = diff.MembersModified;
        if (diff.DesiresOld != null) result["desires"] = new { old = diff.DesiresOld, @new = diff.DesiresNew };

        return result;
    }

    private static int CountChangedFields(Diff diff)
    {
        var n = 0;
        if (diff.Head.TryGetValue("Changes", out var h)) n += h.Count;
        if (diff.Registration.TryGetValue("Changes", out var r)) n += r.Count;
        n += diff.MembersAdded.Count;
        n += diff.MembersRemoved.Count;
        foreach (var kv in diff.MembersModified) n += kv.Value.Count;
        if (diff.DesiresOld != null) n += 1;
        return n;
    }

    private static object? ReadField(object obj, string field)
    {
        var prop = obj.GetType().GetProperty(field);
        if (prop == null) return null;
        return prop.GetValue(obj);
    }

    private static object? ReadFieldFromViewModel(RegistrationViewModel vm, string field)
    {
        var prop = typeof(RegistrationViewModel).GetProperty(field);
        if (prop == null) return null;
        return prop.GetValue(vm);
    }

    private static object? ReadPersonFieldFromViewModel(PersonViewModel vm, string field)
    {
        var prop = typeof(PersonViewModel).GetProperty(field);
        if (prop == null) return null;
        return prop.GetValue(vm);
    }

    private static object? ReadMemberFieldFromViewModel(MemberViewModel vm, string field)
    {
        var prop = typeof(MemberViewModel).GetProperty(field);
        if (prop == null) return null;
        return prop.GetValue(vm);
    }

    private static object? Normalize(object? v)
    {
        if (v == null) return null;
        if (v is string s) return string.IsNullOrWhiteSpace(s) ? null : s;
        if (v is DateTime dt) return dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        if (v is bool b) return b;
        if (v is int i) return i;
        if (v is long l) return l;
        if (v is decimal m) return m;
        return v;
    }
}
