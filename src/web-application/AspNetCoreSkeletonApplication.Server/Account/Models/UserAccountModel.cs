namespace AspNetCoreSkeletonApplication.Server.Account.Models;

/// <summary>관리 화면이 찾은 사용자 — 그룹에 넣을 때 쓰는 ID 와 이메일만.</summary>
public sealed record UserAccountModel(long Id, string Email);
