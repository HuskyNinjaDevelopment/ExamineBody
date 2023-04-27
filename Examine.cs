using CitizenFX.Core;
using CitizenFX.Core.Native;
using FivePD.API;
using FivePD.API.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExamineBody
{
    internal class Examine: Plugin
    {
        private Ped _closestBody = null;
        private bool _foundBody = false;
        private List<Ped> _assessedBodies = new List<Ped>();
        private bool _isOnDuty = false;

        private List<int> _causeOfDeathMeele = new List<int>()
        {
            -1569615261, 1737195953, 1317494643, -1786099057, 1141786504, -2067956739, -868994466
        };
        private List<int> _causeOfDeathKnife = new List<int>()
        {
            -1716189206, 1223143800, -1955384325, -1833087301, 910830060
        };
        private List<int> _causeOfDeathGun = new List<int>()
        {
            453432689, 1593441988, 584646201, -1716589765, 324215364, 736523883, -270015777, -1074790547, -2084633992, -1357824103, -1660422300, 2144741730, 487013001, 2017895192, -494615257, -1654528753, 100416529, 205991906, 1119849093, 177293209, -952879014
        };
        private List<int> _causeOfDeathAnimal = new List<int>()
        {
             -100946242, 148160082
        };
        private List<int> _causeOfDeathFall = new List<int>()
        {
            -842959696
        };
        private List<int> _causeOfDeathExplosion = new List<int>()
        {
            -1568386805, 1305664598, -1312131151, 375527679, 324506233, 1752584910, -1813897027, 741814745, -37975472, 539292904, 341774354, -1090665087
        };
        private List<int> _causeOfDeathGas = new List<int>()
        {
            -1600701090
        };
        private List<int> _causeOfDeathBurn = new List<int>()
        {
            615608432, 883325847, -544306709
        };
        private List<int> _causeOfDeathDrown = new List<int>()
        {
            -10959621, 1936677264
        };
        private List<int> _causeOfDeathVehicle = new List<int>()
        {
            133987706, -1553120962
        };

        internal Examine()
        {
            Events.OnDutyStatusChange += OnDutyStatusChange;

            Tick += ScanForBody;
            Tick += ExamineBody;
            Tick += NearBody;
        }
        private async Task OnDutyStatusChange(bool duty)
        {
            _isOnDuty = (duty == true) ? _isOnDuty = true : _isOnDuty = false;

            await Task.FromResult(0);
        }
        private async Task ScanForBody()
        {
            if(_foundBody || !_isOnDuty) { return; }

            await Delay(250);
            var peds = World.GetAllPeds().Where(p => World.GetDistance(Game.PlayerPed.Position, p.Position) <= 3f).OrderBy(p => World.GetDistance(Game.PlayerPed.Position, p.Position));

            foreach(var p in peds)
            {
                if(p.IsDead && !_assessedBodies.Contains(p))
                {
                    _closestBody = p;
                    _foundBody = true;
                }
            }

            await Task.FromResult(0);
        }
        private async Task NearBody()
        {
            if(_closestBody == null) { return; }
            if(World.GetDistance(Game.PlayerPed.Position, _closestBody.Position) > 10f) 
            { 
                _closestBody = null; 
                _foundBody = false; 
            }

            await Task.FromResult(0);
        }
        private async Task ExamineBody()
        {
            if (!_foundBody || _closestBody == null || !_isOnDuty) { return; }

            if(World.GetDistance(Game.PlayerPed.Position, _closestBody.Position) > 2f || Game.PlayerPed.IsInVehicle()) { return; }
            Draw3dText("Press ~r~[H]~s~ to ~y~Examine Body", _closestBody.Position);

            if(Game.IsControlJustPressed(0, (Control)74))
            {
                PlayAssessmentAnimation();
                Tick -= ExamineBody;
            }


            await Task.FromResult(0);
        }
        private void ShowNotification(string msg)
        {
            API.SetNotificationTextEntry("STRING");
            API.AddTextComponentString(msg);
            API.DrawNotification(true, true);
        }
        private void Draw3dText(string msg, Vector3 pos)
        {
            float textX = 0f, textY = 0f;
            Vector3 camLoc;
            API.World3dToScreen2d(pos.X, pos.Y, pos.Z, ref textX, ref textY);
            camLoc = API.GetGameplayCamCoords();
            float distance = API.GetDistanceBetweenCoords(camLoc.X, camLoc.Y, camLoc.Z, pos.X, pos.Y, pos.Z, true);
            float scale = (1 / distance) * 2;
            float fov = (1 / API.GetGameplayCamFov()) * 100;
            scale = scale * fov * 0.5f;

            API.SetTextScale(0.0f, scale);
            API.SetTextFont(0);
            API.SetTextProportional(true);
            API.SetTextColour(255, 255, 255, 215);
            API.SetTextDropshadow(0, 0, 0, 0, 255);
            API.SetTextEdge(2, 0, 0, 0, 150);
            API.SetTextDropShadow();
            API.SetTextOutline();
            API.SetTextEntry("STRING");
            API.SetTextCentre(true);
            API.AddTextComponentString(msg);
            API.DrawText(textX, textY);
        }
        private async void PlayAssessmentAnimation()
        {
            List<string> idles = new List<string>() { "idle_a", "idle_b", "idle_c" };

            string enterDict = "amb@medic@standing@kneel@enter";
            string enterName = "enter";

            string exitDict = "amb@medic@standing@tendtodead@exit";
            string exitName = "exit";

            string idleDict = "amb@medic@standing@tendtodead@idle_a";
            string idleName = idles.SelectRandom();

            API.RequestAnimDict(enterDict);
            while (!API.HasAnimDictLoaded(enterDict)) { await Delay(10); }

            API.RequestAnimDict(exitDict);
            while (!API.HasAnimDictLoaded(exitDict)) { await Delay(10); }

            API.RequestAnimDict(idleDict);
            while (!API.HasAnimDictLoaded(idleDict)) { await Delay(10); }

            TaskSequence ts = new TaskSequence();
            ts.AddTask.PlayAnimation(enterDict, enterName);
            ts.AddTask.PlayAnimation(idleDict, idleName, 8.0f, 13340, AnimationFlags.Loop);
            ts.AddTask.PlayAnimation(exitDict, exitName);
            ts.Close();

            Game.PlayerPed.AlwaysKeepTask = true;
            Game.PlayerPed.Weapons.Give(WeaponHash.Unarmed, 1, true, true);
            Game.PlayerPed.Task.PerformSequence(ts);

            await Delay(18000);

            int hash = API.GetPedCauseOfDeath(_closestBody.Handle);

            if(_causeOfDeathMeele.Contains(hash))
            {
                ShowNotification("After careful examination the cause of death appears to be a ~y~blunt weapon");
            }
            else if(_causeOfDeathKnife.Contains(hash))
            {
                ShowNotification("After careful examination the cause of death appears to be a ~y~stab wound");
            }
            else if(_causeOfDeathGun.Contains(hash))
            {
                ShowNotification("After careful examination the cause of death appears to be a ~y~gunshot wound");
            }
            else if(_causeOfDeathGas.Contains(hash))
            {
                ShowNotification("After careful examination the cause of death appears to be ~y~exposure to toxic gas");
            }
            else if(_causeOfDeathFall.Contains(hash))
            {
                ShowNotification("After careful examination the cause of death appears to be ~y~falling");
            }
            else if(_causeOfDeathExplosion.Contains(hash))
            {
                ShowNotification("After careful examination the cause of death appears to be from ~y~an explosion");
            }
            else if(_causeOfDeathDrown.Contains(hash))
            {
                ShowNotification("After careful examination the cause of death appears to be from ~y~drowning");
            }
            else if(_causeOfDeathBurn.Contains(hash))
            {
                ShowNotification("After careful examination the cause of death appears to be from ~y~3rd Degree Burns");
            }
            else if(_causeOfDeathAnimal.Contains(hash))
            {
                ShowNotification("After careful examination the cause of death appears to be from ~y~an animal");
            }
            else if(_causeOfDeathVehicle.Contains(hash))
            {
                ShowNotification("After careful examination it appears that the cause of death was from a ~y~vehicle impact");
            }
            else
            {
                ShowNotification("After careful examination the cause of death appears to be ~y~unknown~s~ at this time");
            }

            _assessedBodies.Add(_closestBody);

            _closestBody = null;
            _foundBody = false;

            Tick += ExamineBody;

            await Task.FromResult(0);
        }
    }
}
