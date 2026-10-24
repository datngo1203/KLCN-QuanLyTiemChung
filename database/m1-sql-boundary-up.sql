-- REVIEWED DEPLOYMENT MIGRATION. DBA only, explicit synthetic/approved target DB.
-- No login/user creation here. Never run at API/CLI startup. No EXECUTE AS OWNER.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID('dbo.IdentityRecoveryDatabase') IS NULL THROW 51020,'M1 migration required',1;
IF DATABASE_PRINCIPAL_ID('m1_app') IS NULL CREATE ROLE m1_app AUTHORIZATION dbo;
IF DATABASE_PRINCIPAL_ID('m1_operator') IS NULL CREATE ROLE m1_operator AUTHORIZATION dbo;
-- Deployment seeds permission codes; runtime operator cannot create the dictionary.
INSERT dbo.QuyenHan(Code,TenQuyen,MoTa)
 SELECT v.Code,v.Code,v.Code FROM (VALUES
 ('XemTongQuan'),('QuanLyLichHen'),('GuiNhacLich'),('QuanLyKhuyenMai'),('XemBaoCao'),('XemThanhToan'),
 ('GhiThanhToan'),('QuanLyTaiKhoan'),('QuanLyDanhMucChuyenMon'),('QuanLyDanhMucGia'),('XemKho'),('XemHoSo')) v(Code)
 WHERE NOT EXISTS(SELECT 1 FROM dbo.QuyenHan p WHERE p.Code=v.Code);
EXEC(N'CREATE OR ALTER TRIGGER dbo.M1GuardAccount ON dbo.TaiKhoan
WITH EXECUTE AS CALLER AFTER INSERT,UPDATE,DELETE AS
BEGIN
 SET NOCOUNT ON;
 IF COALESCE(IS_ROLEMEMBER(''m1_operator''),0)<>1 AND
  (EXISTS(SELECT 1 FROM inserted WHERE AccountType=''Employee'' OR MaNhanVien IS NOT NULL OR AdministrativeDuty<>''None'')
   OR EXISTS(SELECT 1 FROM deleted WHERE AccountType=''Employee'' OR MaNhanVien IS NOT NULL OR AdministrativeDuty<>''None''))
  THROW 51021,''Employee mutation requires SQL operator'',1;
END');
EXEC(N'CREATE OR ALTER TRIGGER dbo.M1GuardAudit ON dbo.SecurityAudit
WITH EXECUTE AS CALLER AFTER INSERT AS
BEGIN
 SET NOCOUNT ON;
 IF COALESCE(IS_ROLEMEMBER(''m1_operator''),0)<>1 AND EXISTS
  (SELECT 1 FROM inserted WHERE Action IN (''adminBootstrapped'',''adminRecovered'',''employeeProvisioned'',
   ''employeePasswordReset'',''accountStatusChanged'',''employeeStatusChanged'',''permissionsChanged''))
  THROW 51022,''Administrative audit requires SQL operator'',1;
END');
-- App: only objects queried by current M1 paths, no broad schema grants.
GRANT CONNECT TO m1_app;
GRANT SELECT,INSERT,UPDATE ON dbo.TaiKhoan TO m1_app;
GRANT SELECT ON dbo.NhanVien TO m1_app;
GRANT SELECT ON dbo.QuyenHan TO m1_app;
GRANT SELECT ON dbo.TaiKhoan_QuyenHan TO m1_app;
GRANT SELECT ON dbo.AccountProfileLink TO m1_app;
GRANT SELECT,INSERT ON dbo.KhachHang TO m1_app;
GRANT SELECT,INSERT ON dbo.NguoiDuocTiem TO m1_app;
GRANT SELECT,INSERT,UPDATE,DELETE ON dbo.IdentitySession TO m1_app;
GRANT SELECT,INSERT,UPDATE,DELETE ON dbo.RefreshCredential TO m1_app;
GRANT SELECT,INSERT,UPDATE,DELETE ON dbo.EmailVerification TO m1_app;
GRANT SELECT,INSERT,UPDATE,DELETE ON dbo.IdentityMailOutbox TO m1_app;
GRANT INSERT ON dbo.SecurityAudit TO m1_app;
GRANT SELECT ON dbo.VacXin TO m1_app;
DENY INSERT,UPDATE,DELETE ON dbo.NhanVien TO m1_app;
DENY INSERT,UPDATE,DELETE ON dbo.QuyenHan TO m1_app;
DENY INSERT,UPDATE,DELETE ON dbo.TaiKhoan_QuyenHan TO m1_app;
DENY INSERT,UPDATE,DELETE ON dbo.AccountProfileLink TO m1_app;
DENY SELECT,INSERT,UPDATE,DELETE ON dbo.IdentityRecoveryDatabase TO m1_app;
DENY SELECT,INSERT,UPDATE,DELETE ON dbo.IdentityRecoveryOperation TO m1_app;
DENY DELETE ON dbo.TaiKhoan TO m1_app;
DENY SELECT,UPDATE,DELETE ON dbo.SecurityAudit TO m1_app;
GRANT CONNECT TO m1_operator;
GRANT SELECT,INSERT,UPDATE ON dbo.TaiKhoan TO m1_operator;
GRANT SELECT ON dbo.NhanVien TO m1_operator;
GRANT UPDATE (TrangThai) ON dbo.NhanVien TO m1_operator;
GRANT SELECT ON dbo.QuyenHan TO m1_operator;
GRANT SELECT,INSERT,DELETE ON dbo.TaiKhoan_QuyenHan TO m1_operator;
GRANT SELECT,UPDATE ON dbo.IdentitySession TO m1_operator;
GRANT SELECT,UPDATE ON dbo.RefreshCredential TO m1_operator;
GRANT SELECT ON dbo.IdentityRecoveryDatabase TO m1_operator;
GRANT SELECT,INSERT ON dbo.IdentityRecoveryOperation TO m1_operator;
GRANT SELECT ON dbo.__EFMigrationsHistory TO m1_operator;
GRANT INSERT ON dbo.SecurityAudit TO m1_operator;
GRANT VIEW DEFINITION ON OBJECT::dbo.TaiKhoan TO m1_operator;
GRANT VIEW DEFINITION ON OBJECT::dbo.SecurityAudit TO m1_operator;
GRANT VIEW DEFINITION ON ROLE::m1_operator TO m1_operator;
GRANT VIEW DEFINITION ON ROLE::m1_app TO m1_operator;
DENY DELETE ON dbo.TaiKhoan TO m1_operator;
DENY UPDATE,DELETE ON dbo.IdentityRecoveryOperation TO m1_operator;
DENY INSERT,UPDATE,DELETE ON dbo.IdentityRecoveryDatabase TO m1_operator;
DENY UPDATE,DELETE ON dbo.SecurityAudit TO m1_operator;
-- No runtime role may change guards, membership, owner or execution identity.
DENY ALTER,TAKE OWNERSHIP ON SCHEMA::dbo TO m1_app;
DENY ALTER,TAKE OWNERSHIP ON SCHEMA::dbo TO m1_operator;
DENY ALTER ANY ROLE,ALTER ANY USER TO m1_app;
DENY ALTER ANY ROLE,ALTER ANY USER TO m1_operator;
-- SQL Server exposes IMPERSONATE on USER, not a database-wide ANY USER permission.
-- Runtime users receive no impersonation grants; explicitly deny privileged built-ins.
DENY IMPERSONATE ON USER::dbo TO m1_app;
DENY IMPERSONATE ON USER::guest TO m1_app;
DENY IMPERSONATE ON USER::dbo TO m1_operator;
DENY IMPERSONATE ON USER::guest TO m1_operator;
IF EXISTS(SELECT 1 FROM sys.extended_properties WHERE class=0 AND name='KLCN.M1.SqlBoundary')
 EXEC sys.sp_updateextendedproperty @name=N'KLCN.M1.SqlBoundary',@value=N'1';
ELSE EXEC sys.sp_addextendedproperty @name=N'KLCN.M1.SqlBoundary',@value=N'1';
COMMIT;
