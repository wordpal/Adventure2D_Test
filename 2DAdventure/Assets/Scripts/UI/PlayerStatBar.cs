using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStatBar : MonoBehaviour
{
    public Image healthImage;
    public Image healthDelayImage;
    public Image powerImage;

    public bool isRecovering;
    public Character currentCharacter;
    private void Update()
    {
        //只要减少血量，红条就随着时间下降
        if (healthImage.fillAmount < healthDelayImage.fillAmount)
        {
            healthDelayImage.fillAmount -= Time.deltaTime;
        }

        if (isRecovering)
        {
            float percentage = currentCharacter.currentPower / currentCharacter.maxPower;
            powerImage.fillAmount = percentage;
            if (percentage >= 1)
            {
                isRecovering = false;
                return;
            }
        }
    }

    /// <summary>
    /// 接收Health的变更百分比
    /// </summary>
    /// <param name="persentage">currentHealth / maxHealth</param>
    public void OnhealthChange(float persentage)
    {
        healthImage.fillAmount = persentage;
    }

    public void OnPowerChange(Character character) 
    {
        isRecovering = true;
        currentCharacter = character;
    }

}
