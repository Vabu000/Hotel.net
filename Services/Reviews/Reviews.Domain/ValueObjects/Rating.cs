using HotelReviews.Domain.Exceptions;

namespace HotelReviews.Domain.ValueObjects;

public sealed class Rating : ValueObject
{
    public int Value { get; }

    private Rating(int value)
    {
        Value = value;
    }

    public static Rating Create(int value)
    {
        if (value < 1 || value > 5)
        {
            throw new ValidationException($"Рейтинг повинен бути від 1 до 5. Отримано: {value}");
        }

        return new Rating(value);
    }

    public string ToStars() => new string('⭐', Value);
    public bool IsHighRating => Value >= 4;
    public bool IsLowRating => Value <= 2;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => $"{Value}/5";

    public static implicit operator int(Rating rating) => rating.Value;
}
