#region "copyright"

/*
    Copyright © 2024 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using EmbedIO;
using EmbedIO.Routing;
using NINA.Core.Utility;
using NINA.Equipment.Equipment.MyFlatDevice;
using NINA.Equipment.Interfaces.Mediator;
using ninaAPI.Utility;
using System;
using System.Threading.Tasks;

namespace ninaAPI.WebService.V2
{
    public partial class ControllerV2
    {
        private static readonly Func<object, EventArgs, Task> FlatDeviceConnectedHandler = async (_, _) => await WebSocketV2.SendAndAddEvent("FLAT-CONNECTED");
        private static readonly Func<object, EventArgs, Task> FlatDeviceDisconnectedHandler = async (_, _) => await WebSocketV2.SendAndAddEvent("FLAT-DISCONNECTED");

        public static void StartFlatDeviceWatchers()
        {
            AdvancedAPI.Controls.FlatDevice.Connected += FlatDeviceConnectedHandler;
            AdvancedAPI.Controls.FlatDevice.Disconnected += FlatDeviceDisconnectedHandler;
        }

        public static void StopFlatDeviceWatchers()
        {
            AdvancedAPI.Controls.FlatDevice.Connected -= FlatDeviceConnectedHandler;
            AdvancedAPI.Controls.FlatDevice.Disconnected -= FlatDeviceDisconnectedHandler;
        }


        [Route(HttpVerbs.Get, "/equipment/flatdevice/info")]
        public void FlatDeviceInfo()
        {
            HttpResponse response = new HttpResponse();

            try
            {
                IFlatDeviceMediator flat = AdvancedAPI.Controls.FlatDevice;

                FlatDeviceInfo info = flat.GetInfo();
                response.Response = info;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/equipment/flatdevice/connect")]
        public async Task FlatDeviceConnect()
        {
            HttpResponse response = new HttpResponse();

            try
            {
                IFlatDeviceMediator flat = AdvancedAPI.Controls.FlatDevice;

                if (!flat.GetInfo().Connected)
                {
                    await flat.Rescan();
                    await flat.Connect();
                }
                response.Response = "Flatdevice connected";
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/equipment/flatdevice/disconnect")]
        public async Task FlatDeviceDisconnect()
        {
            HttpResponse response = new HttpResponse();

            try
            {
                IFlatDeviceMediator flat = AdvancedAPI.Controls.FlatDevice;

                if (flat.GetInfo().Connected)
                {
                    await flat.Disconnect();
                }
                response.Response = "Flatdevice disconnected";
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }
    }
}
