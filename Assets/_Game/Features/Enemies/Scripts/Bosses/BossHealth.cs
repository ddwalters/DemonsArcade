using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public class BossHealth : MonoBehaviour
{
    private float bossHealth;
    private float currentHealth;
    [SerializeField] Slider mainSlider;
    [SerializeField] Animator mainAnim;
    [SerializeField] bool hasMultipleBars;
    [SerializeField] Slider slider1;
    [SerializeField] Animator anim1;
    [SerializeField] Slider slider2;
    [SerializeField] Animator anim2;

    private void Start()
    {
        mainAnim = gameObject.GetComponent<Animator>();
    }

    public void makeBar(float health)
    {
        bossHealth = health;
        mainSlider.maxValue = bossHealth;
        mainSlider.value = bossHealth;
        mainAnim.SetTrigger("On");
    }
}
