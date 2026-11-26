using HotelReviews.Domain.Common;
using HotelReviews.Domain.Exceptions;
using HotelReviews.Domain.ValueObjects;

namespace HotelReviews.Domain.Entities;

public class Request : BaseEntity
{
    public int ClientId { get; private set; }

    public int RoomId { get; private set; }

    public string RequestText { get; private set; }

    public RequestStatus Status { get; private set; }

    public DateTime Date { get; private set; }

    public int Priority { get; private set; }

    public string Category { get; private set; }

    public string? Response { get; private set; }

    public DateTime? ResponseDate { get; private set; }

    public int? HandledBy { get; private set; }

    private Request()
    {
        RequestText = string.Empty;
        Status = RequestStatus.Pending;
        Category = string.Empty;
    }

    private Request(int clientId, int roomId, string requestText, string category, int priority)
    {
        ClientId = clientId;
        RoomId = roomId;
        RequestText = requestText;
        Category = category;
        Priority = priority;
        Status = RequestStatus.Pending;
        Date = DateTime.UtcNow;
    }

    public static Request Create(int clientId, int roomId, string requestText, string category = "general", int priority = 3)
    {
        if (clientId <= 0)
            throw new ValidationException(nameof(ClientId), "ID клієнта повинен бути додатнім числом");

        if (roomId <= 0)
            throw new ValidationException(nameof(RoomId), "ID кімнати повинен бути додатнім числом");

        if (string.IsNullOrWhiteSpace(requestText))
            throw new ValidationException(nameof(RequestText), "Текст запиту не може бути порожнім");

        if (requestText.Length < 10)
            throw new ValidationException(nameof(RequestText), "Запит повинен містити щонайменше 10 символів");

        if (requestText.Length > 5000)
            throw new ValidationException(nameof(RequestText), "Запит не може перевищувати 5000 символів");

        if (priority < 1 || priority > 5)
            throw new ValidationException(nameof(Priority), "Пріоритет повинен бути від 1 до 5");

        if (string.IsNullOrWhiteSpace(category))
            throw new ValidationException(nameof(Category), "Категорія не може бути порожньою");

        return new Request(clientId, roomId, requestText, category, priority);
    }

    public void UpdateText(string newRequestText)
    {
        if (Status != RequestStatus.Pending)
            throw new ConflictException("Неможливо редагувати запит у статусі: " + Status);

        if (string.IsNullOrWhiteSpace(newRequestText))
            throw new ValidationException(nameof(RequestText), "Текст запиту не може бути порожнім");

        if (newRequestText.Length < 10)
            throw new ValidationException(nameof(RequestText), "Запит повинен містити щонайменше 10 символів");

        if (newRequestText.Length > 5000)
            throw new ValidationException(nameof(RequestText), "Запит не може перевищувати 5000 символів");

        RequestText = newRequestText;
        UpdateTimestamp();
    }

    public void ChangeStatus(string newStatus)
    {
        var requestedStatus = RequestStatus.Create(newStatus);

        if (Status == requestedStatus)
            throw new ConflictException($"Запит вже має статус '{newStatus}'");

        if (!Status.CanTransitionTo(requestedStatus))
            throw new ConflictException($"Неможливо змінити статус з '{Status}' на '{newStatus}'");

        Status = requestedStatus;
        UpdateTimestamp();
    }

    public void Approve(int handledByEmployeeId)
    {
        if (handledByEmployeeId <= 0)
            throw new ValidationException(nameof(handledByEmployeeId), "ID співробітника повинен бути додатнім числом");

        ChangeStatus("approved");
        HandledBy = handledByEmployeeId;
    }

    public void Reject(int handledByEmployeeId, string responseText)
    {
        if (handledByEmployeeId <= 0)
            throw new ValidationException(nameof(handledByEmployeeId), "ID співробітника повинен бути додатнім числом");

        if (string.IsNullOrWhiteSpace(responseText))
            throw new ValidationException(nameof(responseText), "Вкажіть причину відхилення");

        ChangeStatus("rejected");
        HandledBy = handledByEmployeeId;
        AddResponse(responseText);
    }

    public void AddResponse(string responseText)
    {
        if (string.IsNullOrWhiteSpace(responseText))
            throw new ValidationException(nameof(responseText), "Відповідь не може бути порожньою");

        if (responseText.Length > 5000)
            throw new ValidationException(nameof(responseText), "Відповідь не може перевищувати 5000 символів");

        Response = responseText;
        ResponseDate = DateTime.UtcNow;
        UpdateTimestamp();
    }

    public void StartProcessing(int employeeId)
    {
        if (employeeId <= 0)
            throw new ValidationException(nameof(employeeId), "ID співробітника повинен бути додатнім числом");

        ChangeStatus("in_progress");
        HandledBy = employeeId;
    }
    public void Complete(string responseText)
    {
        if (string.IsNullOrWhiteSpace(responseText))
            throw new ValidationException(nameof(responseText), "Вкажіть результат обробки");

        ChangeStatus("completed");
        AddResponse(responseText);
    }

    public void ChangePriority(int newPriority)
    {
        if (newPriority < 1 || newPriority > 5)
            throw new ValidationException(nameof(Priority), "Пріоритет повинен бути від 1 до 5");

        Priority = newPriority;
        UpdateTimestamp();
    }

    public bool IsHighPriority => Priority >= 4;

    public bool IsCritical => Status == RequestStatus.Pending &&
                              DateTime.UtcNow - Date > TimeSpan.FromHours(24);

    public bool HasResponse => !string.IsNullOrEmpty(Response);
}
