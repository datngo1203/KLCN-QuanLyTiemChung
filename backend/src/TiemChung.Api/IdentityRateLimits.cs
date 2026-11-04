using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace TiemChung.Api;

public sealed class IdentityRateLimits
{
    private readonly object gate=new();
    private readonly Dictionary<string,Queue<DateTimeOffset>> buckets=[];
    // Process-local limits. Email keys are hashed and bucket keys are never logged.
    public void Check(string action,string identifier,string ip)
    {
        var now=DateTimeOffset.UtcNow;
        var email=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(IdentityRegistration.Normalize(identifier))));
        lock(gate) {
            RemoveStaleBuckets(now);
            if(action=="login") Take("login:"+email+":"+ip,10,TimeSpan.FromMinutes(15),now);
            else if(action=="changePassword") Take("password:"+ip,10,TimeSpan.FromMinutes(15),now);
            else if(action=="confirm") Take("confirm:"+ip,10,TimeSpan.FromMinutes(15),now);
            else {
                Take("mail-ip:"+ip,5,TimeSpan.FromHours(1),now);
                Take("mail-email:"+email,3,TimeSpan.FromHours(1),now);
                if(action=="resend") Take("resend-cooldown:"+email,1,TimeSpan.FromSeconds(60),now);
            }
        }
    }

    public void CheckProfileClaimIp(string ip) => CheckClaimBucket("profile-claim-ip:" + ip, 30);

    public void CheckProfileClaimAccount(int accountId) =>
        CheckClaimBucket("profile-claim-account:" + accountId.ToString(CultureInfo.InvariantCulture), 6);

    private void CheckClaimBucket(string key, int limit)
    {
        var now = DateTimeOffset.UtcNow;
        lock (gate)
        {
            RemoveStaleBuckets(now);
            Take(key, limit, TimeSpan.FromMinutes(15), now);
        }
    }

    private void RemoveStaleBuckets(DateTimeOffset now)
    {
        foreach (var key in buckets.Where(x => x.Value.Count == 0 || now - x.Value.Last() > TimeSpan.FromHours(1)).Select(x => x.Key).ToArray())
            buckets.Remove(key);
    }

    private void Take(string key,int limit,TimeSpan window,DateTimeOffset now)
    {
        if(!buckets.TryGetValue(key,out var queue)) buckets[key]=queue=new();
        while(queue.TryPeek(out var first) && now-first>=window) queue.Dequeue();
        if(queue.Count>=limit) throw new IdentityFailure(429,"rateLimited");
        queue.Enqueue(now);
    }
}
