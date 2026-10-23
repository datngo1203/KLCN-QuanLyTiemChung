using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace TiemChung.IdentityOperator;

// No caller-selected path, environment configuration or development bypass.
public sealed record ProtectedOperatorPolicy(string[] AllowedSids,string Server,string Database,string ExpectedSecret,RecoveryManifest? Manifest)
{
    public string PrincipalSid { get; private set; }="";
    public string ConnectionString => new SqlConnectionStringBuilder { DataSource=Server,InitialCatalog=Database,
        IntegratedSecurity=true,Encrypt=true,TrustServerCertificate=false }.ConnectionString;

    public static ProtectedOperatorPolicy Load()
    {
        if(!OperatingSystem.IsWindows()) throw new InvalidOperationException("Windows operator required.");
        return LoadWindows();
    }

    [SupportedOSPlatform("windows")]
    private static ProtectedOperatorPolicy LoadWindows()
    {
        // KF_FLAG_DEFAULT_PATH ignores per-user folder redirection and caller environment.
        var folder=new Guid("62AB5D82-FDC1-4DC3-A9DD-070D1D495D97");
        Marshal.ThrowExceptionForHR(SHGetKnownFolderPath(ref folder,0x400,IntPtr.Zero,out var pointer));
        string root;
        try { root=Marshal.PtrToStringUni(pointer)??throw new InvalidOperationException(); }
        finally { Marshal.FreeCoTaskMem(pointer); }
        var path=Path.Combine(root,"KLCN090","IdentityOperator","policy.json");
        CheckChain(path);
        CheckChain(typeof(ProtectedOperatorPolicy).Assembly.Location);
        var policy=JsonSerializer.Deserialize<ProtectedOperatorPolicy>(File.ReadAllText(path))??throw new InvalidOperationException("Policy unavailable.");
        var sid=WindowsIdentity.GetCurrent().User?.Value??throw new InvalidOperationException("SID unavailable.");
        if(policy.AllowedSids is null || !policy.AllowedSids.Contains(sid,StringComparer.Ordinal) ||
            string.IsNullOrWhiteSpace(policy.Server) || string.IsNullOrWhiteSpace(policy.Database) ||
            Convert.FromBase64String(policy.ExpectedSecret).Length!=32) throw new InvalidOperationException("Policy denied.");
        policy.PrincipalSid=sid;
        return policy;
    }

    [SupportedOSPlatform("windows")]
    private static void CheckChain(string path)
    {
        var current=Path.GetFullPath(path);

        while(true) {
            FileSystemInfo item=Directory.Exists(current)?new DirectoryInfo(current):new FileInfo(current);
            if(!item.Exists || (item.Attributes&FileAttributes.ReparsePoint)!=0) throw new InvalidOperationException("Untrusted path.");
            FileSystemSecurity acl=item is DirectoryInfo dir?dir.GetAccessControl():((FileInfo)item).GetAccessControl();
            var owner=(acl.GetOwner(typeof(SecurityIdentifier))??throw new InvalidOperationException("Missing owner.")).Value;
            if(owner is not ("S-1-5-18" or "S-1-5-32-544")) throw new InvalidOperationException("Untrusted owner.");
            // The policy/deployment boundary itself must have explicit, protected DACLs.
            // Ancestors are also inspected; inherited write grants remain forbidden.
            if(!acl.AreAccessRulesProtected) throw new InvalidOperationException("Unprotected DACL.");
            var raw=new RawSecurityDescriptor(acl.GetSecurityDescriptorBinaryForm(),0);
            if(raw.DiscretionaryAcl is null) throw new InvalidOperationException("Missing DACL.");
            foreach(GenericAce ace in raw.DiscretionaryAcl) {
                if(ace is not CommonAce known || known.IsCallback || known.AceQualifier is not (AceQualifier.AccessAllowed or AceQualifier.AccessDenied))
                    throw new InvalidOperationException("Unsupported ACL.");
                const int writes=(int)(FileSystemRights.Write|FileSystemRights.Delete|FileSystemRights.DeleteSubdirectoriesAndFiles|FileSystemRights.ChangePermissions|FileSystemRights.TakeOwnership);
                if(known.AceQualifier==AceQualifier.AccessAllowed && (known.AccessMask&(writes|unchecked((int)0x50000000)))!=0 &&
                    known.SecurityIdentifier.Value is not ("S-1-5-18" or "S-1-5-32-544")) throw new InvalidOperationException("Writable path.");
            }
            var parent=Path.GetDirectoryName(current);
            if(string.IsNullOrEmpty(parent) || parent==current) break;
            current=parent;
        }
    }

    [DllImport("shell32.dll",CharSet=CharSet.Unicode)]
    private static extern int SHGetKnownFolderPath(ref Guid folder,uint flags,IntPtr token,out IntPtr path);
}
