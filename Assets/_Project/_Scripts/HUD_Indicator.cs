using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HUD_Indicator : MonoBehaviour
{
    public Transform[] transforms;
    public Transform[] points;
    public Texture targetArrow;

    void Update()
    {
        for (int i = 0; i < transforms.Length; i++)
        {
            Vector3 pos = Camera.main.WorldToScreenPoint(transforms[i].position);
            
            if(pos.z > 0 && pos.x > 0 && pos.x < Screen.width && pos.y > 0 && pos.y < Screen.height)
            {
                points[i].gameObject.SetActive(false);
            }
            else // offscreen
            {
                points[i].gameObject.SetActive(true);

                if (pos.z < 0) pos.z *= -1;

                Vector3 screen_center = new Vector3(Screen.width, Screen.height, 0) / 2;

                // make 00 the center of screen instead of bottom left
                pos -= screen_center;

                // find angle from center of screen to mouse position
                float angle = Mathf.Atan2(pos.y, pos.x);
                angle -= 90 * Mathf.Deg2Rad;

                float cos = Mathf.Cos(angle);
                float sin = -Mathf.Sin(angle);

                pos = screen_center + new Vector3(sin * 150, cos * 150, 0);

                // y = mx + b format
                float m = cos / sin;

                Vector3 screen_bounds = screen_center * 0.9f;

                //check up and down first
                if (cos > 0) pos = new Vector3(screen_bounds.y / m, screen_bounds.y, 0);
                else        pos = new Vector3(-screen_bounds.y / m, -screen_bounds.y, 0); //down

                // if out of bounds, gewt point on appropriate side
                if(pos.x > screen_bounds.x) // out of bounds, must be on the right
                {
                    pos = new Vector3(screen_bounds.x, screen_bounds.x * m, 0);
                }
                else if (pos.x < -screen_bounds.x) // out of bounds left
                {
                    pos = new Vector3(-screen_bounds.x, -screen_bounds.x * m, 0);
                } // else in bounds

                //remove coordinate translation
                pos += screen_center;

                points[i].position = pos;
                points[i].localRotation = Quaternion.Euler(0, 0, angle * Mathf.Rad2Deg);
            }
        }
    }
}
