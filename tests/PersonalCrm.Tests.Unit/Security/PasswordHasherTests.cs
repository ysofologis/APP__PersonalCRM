using FluentAssertions;
using PersonalCrm.Infrastructure.Security;
using Xunit;

namespace PersonalCrm.Tests.Unit.Security;

public class PasswordHasherTests
{
    [Fact]
    public void Hash_produces_argon2id_phc_string()
    {
        
        var encoded = PasswordHasher.Hash("correct horse battery staple");

        encoded.Should().StartWith("$argon2id$v=19$m=65536,t=3,p=4$");
    }

    [Fact]
    public void Verify_accepts_correct_password()
    {
        
        var encoded = PasswordHasher.Hash("correct horse battery staple");

        PasswordHasher.Verify("correct horse battery staple", encoded).Should().BeTrue();
    }

    [Fact]
    public void Verify_rejects_incorrect_password()
    {
        
        var encoded = PasswordHasher.Hash("correct horse battery staple");

        PasswordHasher.Verify("wrong horse battery staple", encoded).Should().BeFalse();
    }

    [Fact]
    public void Verify_rejects_malformed_hash()
    {
        

        PasswordHasher.Verify("any password", "not-a-real-hash").Should().BeFalse();
        PasswordHasher.Verify("any password", "$argon2id$v=19$bad$$$").Should().BeFalse();
    }

    [Fact]
    public void Same_password_two_hashes_differ()
    {
        

        var a = PasswordHasher.Hash("hunter2");
        var b = PasswordHasher.Hash("hunter2");

        a.Should().NotBe(b, "salts must be random");
        PasswordHasher.Verify("hunter2", a).Should().BeTrue();
        PasswordHasher.Verify("hunter2", b).Should().BeTrue();
    }
}
