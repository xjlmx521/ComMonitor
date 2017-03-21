﻿using Mina.Core.Service;
using Mina.Core.Session;
using Mina.Transport.Socket;
using NLog;
using System;
using System.Collections.Generic;

namespace ComMonitor.LocalTools
{
    public class MinaTCPServer
    {
        public delegate void DProcessMessage(byte[] message, HexMessageViewerControl.Direction direction);

        private Logger _logger;

        private TCPServerProtocolManager Manager { get; set; }
        public Boolean Connected { get; set; }
        private List<IoSession> Sessions { get; set; }

        private Int32 _port;
        DProcessMessage CallProcessMessage;

        /// <summary>
        /// Constructor
        /// </summary>
        public MinaTCPServer(Int32 port, DProcessMessage callProcessMessage)
        {
            _logger = LogManager.GetCurrentClassLogger();
            Connected = false;

            Sessions = new List<IoSession>();
            _port = port;
            CallProcessMessage = callProcessMessage;

            Manager = new TCPServerProtocolManager();
            StartMinaListener();
        }

        /// <summary>
        /// StartMinaListener
        /// </summary>
        /// <param name="port"></param>
        private void StartMinaListener()
        {

            Manager.InitializeServer();

            if (Manager.Acceptor == null)
                throw new Exception("This should not happen!");

            Manager.Acceptor.ExceptionCaught += HandleException;
            Manager.Acceptor.SessionOpened += HandeleSessionOpened;
            Manager.Acceptor.SessionClosed += HandeleSessionClosed;
            Manager.Acceptor.SessionIdle += HandleIdle;
            Manager.Acceptor.MessageReceived += HandleReceived;

            Manager.Port = _port;
            Manager.StartServer();
        }

        /// <summary>
        /// Send
        /// </summary>
        /// <param name="message"></param>
        public void Send(byte[] message)
        {
            Manager.Send(message);

            _logger.Info(String.Format("Send data {0} Bytes", message.Length));
            _logger.Trace(String.Format("Send data => {0} | {1} |", ByteArrayToHexString(message), ByteArrayToAsciiString(message)));
        }

        /******************************/
        /*          Events            */
        /******************************/
        #region Events

        /// <summary>
        /// HandleException
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HandleException(Object sender, IoSessionExceptionEventArgs e)
        {
            _logger.Info(String.Format("Exception {0}", e.Exception.Message));
        }

        /// <summary>
        /// HandeleSessionOpened
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HandeleSessionOpened(Object sender, IoSessionEventArgs e)
        {
            Connected = true;
            _logger.Info(String.Format("SessionOpened {0}", e.Session.RemoteEndPoint));
        }

        /// <summary>
        /// HandeleSessionClosed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HandeleSessionClosed(Object sender, IoSessionEventArgs e)
        {
            Connected = false;
            _logger.Info(String.Format("SessionClosed {0}", e.Session.RemoteEndPoint));
        }

        /// <summary>
        /// HandleIdle
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HandleIdle(Object sender, IoSessionIdleEventArgs e)
        {
            _logger.Info(String.Format("Idle {0}", e.Session.BothIdleCount));
        }

        /// <summary>
        /// HandleReceived
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HandleReceived(Object sender, IoSessionMessageEventArgs e)
        {
            var bytes = (byte[])e.Message;

            if (CallProcessMessage != null)
                CallProcessMessage(bytes,HexMessageViewerControl.Direction.In);

            _logger.Info(String.Format("Received data {0} Bytes", bytes.Length));
            _logger.Trace(String.Format("Received data <= {0} | {1} |", ByteArrayToHexString(bytes), ByteArrayToAsciiString(bytes)));
        }
        private static string ByteArrayToHexString(byte[] buf)
        {
            System.Text.StringBuilder hex = new System.Text.StringBuilder(buf.Length * 2);
            foreach (byte b in buf)
                hex.AppendFormat("{0:x2} ", b);
            return hex.ToString();
        }
        private string ByteArrayToAsciiString(byte[] buf)
        {
            char[] carray = new char[buf.Length];
            char c;

            for (int i = 0; i < buf.Length; i++)
            {
                if (33 <= buf[i] && buf[i] <= 127)
                    c = (char)buf[i];
                else
                    c = '.';
                carray[i] = c;
            }

            return new String(carray);
        }

        #endregion
    }
}
