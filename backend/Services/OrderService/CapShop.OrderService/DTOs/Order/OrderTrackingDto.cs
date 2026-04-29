namespace CapShop.OrderService.DTOs.Order
{
    public class OrderTrackingDto
    {
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string TrackingStatus { get; set; } = string.Empty;
        public string CurrentLocationName { get; set; } = string.Empty;
        public string NearestHubName { get; set; } = string.Empty;
        public DateTime EstimatedDeliveryUtc { get; set; }
        public int EstimatedMinutesRemaining { get; set; }
        public TrackingPointDto CurrentLocation { get; set; } = new();
        public TrackingPointDto Destination { get; set; } = new();
        public List<TrackingPointDto> Route { get; set; } = new();
        public List<TrackingEventDto> Events { get; set; } = new();
    }

    public class TrackingPointDto
    {
        public string Name { get; set; } = string.Empty;
        public string Kind { get; set; } = string.Empty;
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsCurrent { get; set; }
    }

    public class TrackingEventDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string LocationName { get; set; } = string.Empty;
        public DateTime OccurredAtUtc { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsCurrent { get; set; }
    }

    public class TrackingHubDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Kind { get; set; } = string.Empty;
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
    }
}
