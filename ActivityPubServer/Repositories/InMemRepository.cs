using ActivityPubServer.Interfaces;
using ActivityPubServer.Model;

namespace ActivityPubServer.Repositories;

public class InMemRepository : IInMemRepository
{
    public Actor GetActor(Guid actorId)
    {
        List<Actor> actors = new()
        {
            new()
            {
                Context = new[]
                {
                    "https://www.w3.org/ns/activitystreams",
                    "https://w3id.org/security/v1"
                },
                Id = new Uri("https://ap.lna-dev.net/actor/037AA32E-0A2F-4785-BC64-4C079E6557C5"),
                Type = "Person",
                PreferredUsername = "Lukas",
                Inbox = new Uri("https://ap.lna-dev.net/inbox"),
                PublicKey = new()
                {
                    Id = new Uri("https://ap.lna-dev.net/actor#main-key"),
                    Owner = new Uri($"https://ap.lna-dev.net/actor/037AA32E-0A2F-4785-BC64-4C079E6557C5"),
                    PublicKeyPem = @"-----BEGIN PUBLIC KEY-----
                MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAz/fLWo1xQpjFmcJrxm1c
                /5OVC5n9pudQUiFuFeOlYhdpVmLFZtVYOCJ/fAg3vsz+pRQH9BVQoAfVHME/Js8i
                IC/1xPyAXaqz+ZxEquL1jFGjI829u2irps/qihXqUzr9j8nd3VgTvy6mQRyZLQ8U
                Dj16ZAcRK0NfIts9nySe7lp0R695b4TUMD/bx2wl2qT0et0puye0jeWfh3F7cwD5
                pciRrAtWVAbe//4RvhVR8I+3H4ue15fpZNO4C7TNk12O+XYjf0pznIZ/hPfMhEYF
                stpraIb3AI4oKgehJBa26JfGO/2ruG3GL1ZF+jg6gjGG0pojBtSUbGElVO1UAg3P
                vwIDAQAB
                -----END PUBLIC KEY-----"
                }
            }
        };

        return actors.FirstOrDefault(i => i.Id.ToString().Split("/").Last().ToUpper() == actorId.ToString().ToUpper());
    }

    public Webfinger? GetWebfinger(string resource)
    {
        List<Webfinger> webfingers = new()
        {
            new Webfinger()
            {
                Subject = "acct:Lukas@ap.lna-dev.net",
                Links = new []
                {
                    new Link()
                    {
                        Rel = "self",
                        Type = "application/activity+json",
                        Href = new Uri("https://ap.lna-dev.net/actor/037AA32E-0A2F-4785-BC64-4C079E6557C5")
                    }
                }
            }
        };
        
        return webfingers.FirstOrDefault(i => i.Subject == resource);
    }
}