using System;

public interface ISaveable
{
    DataDefination GetDataID();
    void RegisterSaveData() => DataManager.instance.RegisterSaveData(this); //Óï·¨ÌÇ
    void UnRegisterSaveData() => DataManager.instance.UnRegisterSaveData(this);

    void GetSaveData(Data data);
    void LoadData(Data data);
}
