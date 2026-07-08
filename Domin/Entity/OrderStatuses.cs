namespace Domin.Entity
{
    public static class OrderStatuses
    {
        public const string PendingReview = "قيد المراجعة";
        public const string Shipping = "قيد الشحن";
        public const string Completed = "مكتمل";
        public const string Cancelled = "ملغي";

        public static readonly string[] Pending =
        {
            PendingReview,
            Shipping
        };

        public static readonly string[] All =
        {
            PendingReview,
            Shipping,
            Completed,
            Cancelled
        };

        public static bool IsAllowed(string? status)
        {
            return !string.IsNullOrWhiteSpace(status)
                && All.Contains(status, StringComparer.Ordinal);
        }
    }
}
