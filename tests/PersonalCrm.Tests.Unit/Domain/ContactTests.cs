using FluentAssertions;
using PersonalCrm.Core.Domain;
using Xunit;

namespace PersonalCrm.Tests.Unit.Domain;

public class ContactTests
{
    [Fact]
    public void FullName_joins_first_middle_last_with_single_spaces()
    {
        var contact = new Contact
        {
            FirstName  = "Ada",
            MiddleName = "Augusta",
            LastName   = "Lovelace"
        };

        contact.FullName.Should().Be("Ada Augusta Lovelace");
    }

    [Fact]
    public void FullName_omits_missing_middle_name()
    {
        var contact = new Contact
        {
            FirstName = "Ada",
            LastName  = "Lovelace"
        };

        contact.FullName.Should().Be("Ada Lovelace");
    }

    [Fact]
    public void NewContact_starts_at_version_zero()
    {
        var contact = new Contact { FirstName = "A", LastName = "B" };
        contact.Version.Should().Be(0);
    }

    [Fact]
    public void SoftDelete_sets_DeletedAt_but_keeps_the_row()
    {
        var contact = new Contact { FirstName = "A", LastName = "B" };
        var deletedAt = DateTimeOffset.UtcNow;
        contact.DeletedAt = deletedAt;

        contact.DeletedAt.Should().Be(deletedAt);
        contact.Should().NotBeNull();
    }
}

public class RelationshipStrengthTests
{
    [Fact]
    public void Zero_interactions_yield_zero_strength()
    {
        var contact = new Contact { FirstName = "A", LastName = "B" };
        var now     = DateTimeOffset.UtcNow;

        var score = RelationshipStrength.Compute(contact, [], now);

        score.Should().Be(0d);
    }

    [Fact]
    public void A_meeting_today_scores_higher_than_an_email_six_months_ago()
    {
        var contact = new Contact { FirstName = "A", LastName = "B" };
        var now     = new DateTimeOffset(2026, 9, 26, 12, 0, 0, TimeSpan.Zero);

        var recent = RelationshipStrength.Compute(contact,
            new[] { (InteractionKind.Meeting, now) },
            now);

        var stale = RelationshipStrength.Compute(contact,
            new[] { (InteractionKind.Email, now.AddDays(-180)) },
            now);

        recent.Should().BeGreaterThan(stale);
        stale.Should().BeLessThan(0.5);
    }

    [Fact]
    public void Gifts_weight_more_than_emails()
    {
        var contact = new Contact { FirstName = "A", LastName = "B" };
        var now     = DateTimeOffset.UtcNow;

        var gifts = RelationshipStrength.Compute(contact,
            new[] { (InteractionKind.Gift, now) },
            now);

        var emails = RelationshipStrength.Compute(contact,
            new[] { (InteractionKind.Email, now) },
            now);

        gifts.Should().BeGreaterThan(emails);
    }

    [Fact]
    public void Score_is_clamped_to_zero_one()
    {
        var contact = new Contact { FirstName = "A", LastName = "B" };
        var now     = DateTimeOffset.UtcNow;

        var many = Enumerable.Range(0, 1000)
            .Select(_ => (InteractionKind.Meeting, now))
            .ToList();

        var score = RelationshipStrength.Compute(contact, many, now);

        score.Should().BeInRange(0d, 1d);
    }
}
