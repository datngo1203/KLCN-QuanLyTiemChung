using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace TiemChung.Api;

public sealed class IdentityRateLimits
{
    private readonly object gate=new();
    private readonly Dictionary<string,Queue<DateTimeOffset>> buckets=[];
    // Process-local technical limits; keys are hashes, never raw identifiers in logs.
    public void Check(string action,string identifier,string ip)
    {
        var now=DateTimeOffset.UtcNow;
        var email=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(IdentityRegistration.Normalize(identifier))));
        lock(gate) {
            foreach(var key in buckets.Where(x=>x.Value.Count==0 || now-x.Value.Last()>TimeSpan.FromHours(1)).Select(x=>x.Key).ToArray()) buckets.Remove(key);
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
    private void Take(string key,int limit,TimeSpan window,DateTimeOffset now)
    {
        if(!buckets.TryGetValue(key,out var queue)) buckets[key]=queue=new();
        while(queue.TryPeek(out var first) && now-first>=window) queue.Dequeue();
        if(queue.Count>=limit) throw new IdentityFailure(429,"rateLimited");
        queue.Enqueue(now);
    }
}
