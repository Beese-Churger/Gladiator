using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuManager : MonoBehaviour
{
    public static MenuManager instance;

    [SerializeField] Menu[] menus;

    private void Awake()
    {
        if (!instance)
            instance = this;
        else
            Destroy(this);
    }
    public void OpenMenu(string menuName)
    {
        for(int i = 0; i < menus.Length; ++i)
        {
            if (menus[i].menuName == menuName)
                menus[i].Open();
            else if(menus[i].open)
                CloseMenu(menus[i]);
        }
    }
    public void OpenMenu(Menu menu)
    { 
        for (int i = 0; i < menus.Length; ++i)
        {
            if (menus[i].open)
                CloseMenu(menus[i]);
        }
        menu.Open();
    }

    public void CloseMenu(Menu menu)
    {
        menu.Close();
    }

    public void Quit()
    {
        Application.Quit();
    }
}
