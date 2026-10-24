-- READ ONLY. Run using existing deployment Windows credential; no login passwords.
SET NOCOUNT ON;
SELECT
 CONVERT(int,SERVERPROPERTY('IsIntegratedSecurityOnly')) AS IntegratedOnly,
 IS_SRVROLEMEMBER('sysadmin') AS DeploymentIsSysadmin,
 HAS_PERMS_BY_NAME(NULL,NULL,'ALTER ANY LOGIN') AS CanCreateTestLogins,
 HAS_PERMS_BY_NAME(NULL,NULL,'CREATE ANY DATABASE') AS CanCreateTestDatabases;
SELECT encrypt_option AS SessionEncryption, auth_scheme AS AuthenticationScheme
 FROM sys.dm_exec_connections WHERE session_id=@@SPID;
SELECT COUNT(*) AS ExistingRunDatabases FROM sys.databases WHERE name IN
 ('KLCN_M1_Test_Boundary_m1br_20261024_01','KLCN_M1_Test_Legacy_m1br_20261024_01');
SELECT COUNT(*) AS ExistingRunLogins FROM sys.server_principals WHERE name IN
 ('m1br_20261024_01_app','m1br_20261024_01_operator');
IF CONVERT(int,SERVERPROPERTY('IsIntegratedSecurityOnly'))<>0
 THROW 51030,'Preflight blocked: SQL authentication unavailable; do not change authentication mode.',1;
IF COALESCE(HAS_PERMS_BY_NAME(NULL,NULL,'ALTER ANY LOGIN'),0)<>1 OR
   COALESCE(HAS_PERMS_BY_NAME(NULL,NULL,'CREATE ANY DATABASE'),0)<>1
 THROW 51031,'Preflight blocked: deployment permissions missing.',1;
IF NOT EXISTS(SELECT 1 FROM sys.dm_exec_connections WHERE session_id=@@SPID AND encrypt_option='TRUE')
 THROW 51032,'Preflight blocked: encrypted session not established.',1;
IF EXISTS(SELECT 1 FROM sys.databases WHERE name IN
 ('KLCN_M1_Test_Boundary_m1br_20261024_01','KLCN_M1_Test_Legacy_m1br_20261024_01')) OR
 EXISTS(SELECT 1 FROM sys.server_principals WHERE name IN
 ('m1br_20261024_01_app','m1br_20261024_01_operator'))
 THROW 51033,'Preflight blocked: a run object name already exists. Do not overwrite/drop.',1;
SELECT 'Preflight eligible; provisioning is a separate controlled step' AS Result;
