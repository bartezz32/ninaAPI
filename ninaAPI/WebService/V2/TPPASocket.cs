﻿#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using EmbedIO.WebSockets;
using Newtonsoft.Json;
using NINA.Core.Utility;
using NINA.Plugin.Interfaces;
using ninaAPI.Utility;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ninaAPI.WebService.V2
{
    public class TPPASocket : WebSocketModule, ISubscriber
    {
        public TPPASocket(string urlPath) : base(urlPath, true)
        {
            AdvancedAPI.Controls.MessageBroker.Subscribe("PolarAlignmentPlugin_PolarAlignment_AlignmentError", this);
        }

        protected override async Task OnMessageReceivedAsync(IWebSocketContext context, byte[] rxBuffer, IWebSocketReceiveResult rxResult)
        {
            string message = Encoding.GetString(rxBuffer); // Do something with it
            string topic;
            string response;

            if (message.Equals("start-alignment"))
            {
                topic = "PolarAlignmentPlugin_DockablePolarAlignmentVM_StartAlignment";
                response = "started procedure";
            }
            else if (message.Equals("stop-alignment"))
            {
                topic = "PolarAlignmentPlugin_DockablePolarAlignmentVM_StopAlignment";
                response = "stopped procedure";
            }
            else if (message.Equals("pause-alignment"))
            {
                topic = "PolarAlignmentPlugin_PolarAlignment_PauseAlignment";
                response = "paused procedure";
            }
            else if (message.Equals("resume-alignment"))
            {
                topic = "PolarAlignmentPlugin_PolarAlignment_ResumeAlignment";
                response = "resumed procedure";
            }
            else
            {
                return;
            }

            Guid correlatedGuid = Guid.NewGuid();
            await AdvancedAPI.Controls.MessageBroker.Publish(new TPPAMessage(correlatedGuid, topic, string.Empty));
            await Send(new HttpResponse()
            {
                Type = HttpResponse.TypeSocket,
                Response = response
            });
        }

        protected override Task OnClientConnectedAsync(IWebSocketContext context)
        {
            Logger.Info("TPPA WebSocket connected " + context.RemoteEndPoint.ToString());
            return Task.CompletedTask;
        }

        public async Task Send(HttpResponse payload)
        {
            Logger.Trace("Sending " + payload.Response + " to TPPA WebSocket");
            foreach (IWebSocketContext context in ActiveContexts)
            {
                Logger.Trace("Sending to " + context.RemoteEndPoint.ToString());
                await SendAsync(context, JsonConvert.SerializeObject(payload));
            }
        }

        // From ISubscriber
        public async Task OnMessageReceived(IMessage message)
        {
            if (message.Topic == "PolarAlignmentPlugin_PolarAlignment_AlignmentError" && message.Version == 1)
            {
                Type t = message.Content.GetType();

                double AzimuthError = (double)t.GetProperty("AzimuthError").GetValue(message.Content, null);
                double AltitudeError = (double)t.GetProperty("AltitudeError").GetValue(message.Content, null);
                double TotalError = (double)t.GetProperty("TotalError").GetValue(message.Content, null);

                await Send(new HttpResponse()
                {
                    Type = HttpResponse.TypeSocket,
                    Response = new Dictionary<string, double>
                    {
                        { "AzimuthError", AzimuthError },
                        { "AltitudeError", AltitudeError },
                        { "TotalError", TotalError },
                    }
                });
            }
        }
    }

    public class TPPAMessage(Guid correlatedGuid, string topic, string content) : IMessage
    {
        public Guid SenderId => Guid.Parse(AdvancedAPI.PluginId);

        public string Sender => nameof(ninaAPI);

        public DateTimeOffset SentAt => DateTime.UtcNow;

        public Guid MessageId => Guid.NewGuid();

        public DateTimeOffset? Expiration => null;

        public Guid? CorrelationId => correlatedGuid;

        public int Version => 1;

        public IDictionary<string, object> CustomHeaders => new Dictionary<string, object>();

        public string Topic => topic;

        public object Content => content;
    }
}
