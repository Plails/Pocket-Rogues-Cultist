using UnityEngine;

namespace FlagellantMod
{
    /// (the user, 2.6.6) the Cultist holds his club in the LEFT hand and the whip in the RIGHT one - as the hunter keeps
    /// his crossbow in the right. The game draws the main weapon in the right hand on every change, so the pictures of
    /// the two hands are set right after it, every frame. With an ordinary weapon in the second hand (an old save)
    /// the game's own way is kept.
    internal class HandsLook : MonoBehaviour
    {
        Character ch;

        void Start() { ch = GetComponent<Character>(); }

        void LateUpdate()
        {
            if (ch == null || !Flag.Active || ch.charClass != CharacterClasses.BERSERKER) return;
            try
            {
                if (ch.ShieldWeapon != null && !Weapons.IsWhip(ch.ShieldWeapon)) return;
                var R = ch.RightHandSprite; var L = ch.LeftHandSprite;
                if (R == null || L == null) return;
                Sprite club = ch.Weapon != null ? ch.Weapon.EquipSprite : null;
                Sprite whip = ch.ShieldWeapon != null ? ch.ShieldWeapon.EquipSprite : null;
                if (L.sprite != club) L.sprite = club;
                if (!L.flipX) L.flipX = true;
                if (R.sprite != whip) R.sprite = whip;
                if (R.flipX) R.flipX = false;
            }
            catch { }
        }
    }
}
