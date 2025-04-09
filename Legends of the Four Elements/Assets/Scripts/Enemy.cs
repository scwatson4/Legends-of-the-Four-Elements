using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public int health = 100;
    internal void ReceiveDamage(int damageToInflict)
    {
        health -= damageToInflict;
    }
}
