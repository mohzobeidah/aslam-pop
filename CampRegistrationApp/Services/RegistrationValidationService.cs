using System;
using System.Diagnostics;
using System.Text;
using CampRegistrationApp.Models.ViewModels;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace CampRegistrationApp.Services
{
    static class RegistrationConstants
    {
        public const string MaritalStatusMarried = "متزوج";
        public const string RelationshipWife = "زوجة";
        public const string HealthStatusSick = "مريض";
        public const string BathroomTypePrivate = "Private";
        public const string BathroomTypeShared = "Shared";

        public static string? NormalizeBathroomType(string? value, bool hasBathroom)
        {
            if (!hasBathroom) return null;
            return value switch
            {
                BathroomTypePrivate or "خاص" => BathroomTypePrivate,
                BathroomTypeShared or "مشترك" => BathroomTypeShared,
                _ => string.IsNullOrWhiteSpace(value) ? null : value
            };
        }
    }

    public interface IRegistrationValidationService
    {
        bool ValidateRegistration(RegistrationViewModel model, ModelStateDictionary modelState, string viewName = "Index");
    }

    public class RegistrationValidationService : IRegistrationValidationService
    {
        static string Normalized(string? s) =>
            (s ?? string.Empty).Normalize(NormalizationForm.FormC).Trim();

        public bool ValidateRegistration(RegistrationViewModel model, ModelStateDictionary modelState, string viewName = "Index")
        {
            if (model == null) return false;

            if (model.Head == null)
            {
                modelState.AddModelError("", "بيانات رب الأسرة مفقودة");
                return false;
            }

            // 1. Marital Status Validation: married requires wife member
            var maritalStatus = Normalized(model.Head.MaritalStatus);
            if (maritalStatus.Contains(RegistrationConstants.MaritalStatusMarried, StringComparison.Ordinal) &&
                !model.Members.Any(m =>
                    Normalized(m.RelationshipToHead)
                        .Contains(RegistrationConstants.RelationshipWife, StringComparison.Ordinal)))
            {
                Debug.WriteLine($"[Validation] Married head without wife. Received MaritalStatus='{maritalStatus}'");
                modelState.AddModelError("", "بما أن الحالة الاجتماعية متزوج، يجب إضافة فرد بصفة زوجة");
                return false;
            }

            // 2. Health Status Validation for Head: sick requires disease or disability
            var healthStatus = Normalized(model.Head.HealthStatus);
            if (healthStatus.Contains(RegistrationConstants.HealthStatusSick, StringComparison.Ordinal) &&
                string.IsNullOrWhiteSpace(model.Head.ChronicDiseases) &&
                string.IsNullOrWhiteSpace(model.Head.DisabilityTypes))
            {
                Debug.WriteLine($"[Validation] Sick head without details. Received HealthStatus='{healthStatus}'");
                modelState.AddModelError("", "بما أن الحالة الصحية مريض، يجب اختيار مرض مزمن أو نوع إعاقة على الأقل لرب الأسرة");
                return false;
            }

            // 3. Health/Healthy contradiction: سليم with diseases
            if (!healthStatus.Contains(RegistrationConstants.HealthStatusSick, StringComparison.Ordinal) &&
                (!string.IsNullOrWhiteSpace(model.Head.ChronicDiseases) ||
                 !string.IsNullOrWhiteSpace(model.Head.DisabilityTypes)))
            {
                Debug.WriteLine($"[Validation] Healthy head with diseases. HealthStatus='{healthStatus}'");
                modelState.AddModelError("", "لا يمكن أن تكون الحالة الصحية سليماً مع وجود أمراض مزمنة أو إعاقات");
                return false;
            }

            // 4. WalletType Validation: required if Wallet is provided
            if (!string.IsNullOrWhiteSpace(model.Wallet) && string.IsNullOrWhiteSpace(model.WalletType))
            {
                modelState.AddModelError("WalletType", "يرجى اختيار نوع المحفظة");
                return false;
            }

            // 4. Marital Status Validation for Members
            for (int i = 0; i < model.Members.Count; i++)
            {
                var m = model.Members[i];
                if (string.IsNullOrWhiteSpace(Normalized(m.MaritalStatus)))
                {
                    var name = Normalized(m.FullName);
                    if (string.IsNullOrEmpty(name))
                        name = $"رقم {i + 1}";
                    modelState.AddModelError("", $"يرجى اختيار الحالة الاجتماعية للفرد: {name}");
                    return false;
                }
            }

            // 5. Health Status Validation for Members: sick requires disease or disability
            for (int i = 0; i < model.Members.Count; i++)
            {
                var m = model.Members[i];
                var memberHealth = Normalized(m.HealthStatus);
                if (memberHealth.Contains(RegistrationConstants.HealthStatusSick, StringComparison.Ordinal) &&
                    string.IsNullOrWhiteSpace(m.ChronicDiseases) &&
                    string.IsNullOrWhiteSpace(m.DisabilityTypes))
                {
                    Debug.WriteLine($"[Validation] Sick member #{i + 1} without details. Received HealthStatus='{memberHealth}'");
                    modelState.AddModelError("", $"الفرد رقم {i + 1}: بما أن الحالة الصحية مريض، يجب اختيار مرض مزمن أو نوع إعاقة على الأقل");
                    return false;
                }
                // 5. Health/Healthy contradiction for members: سليم with diseases
                if (!memberHealth.Contains(RegistrationConstants.HealthStatusSick, StringComparison.Ordinal) &&
                    (!string.IsNullOrWhiteSpace(m.ChronicDiseases) ||
                     !string.IsNullOrWhiteSpace(m.DisabilityTypes)))
                {
                    Debug.WriteLine($"[Validation] Healthy member #{i + 1} with diseases. HealthStatus='{memberHealth}'");
                    modelState.AddModelError("", $"الفرد رقم {i + 1}: لا يمكن أن تكون الحالة الصحية سليماً مع وجود أمراض مزمنة أو إعاقات");
                    return false;
                }

                // 6. MotherIdNumber Validation: required for all registrations
                if (string.IsNullOrWhiteSpace(m.MotherIdNumber))
                {
                    var name = Normalized(m.FullName);
                    if (string.IsNullOrEmpty(name))
                        name = $"رقم {i + 1}";
                    modelState.AddModelError("", $"يرجى إدخال رقم هوية الأم للفرد: {name}");
                    return false;
                }
            }

            return true;
        }
    }
}
