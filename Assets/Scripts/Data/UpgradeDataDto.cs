using System;
using System.Collections.Generic;

//DTO를 명시
[Serializable]
public class UpgradeReqDto
{
    public int userId;
    public string targetId;
    public string materialInfo;
    public float successRate;
}

[Serializable]
public class UpgradeResDto
{
    public bool success;
    public string message;
}