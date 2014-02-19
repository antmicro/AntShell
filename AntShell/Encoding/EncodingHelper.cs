// /********************************************************
//  *   
//  * CONFIDENTIAL  
//  * 
//  * ---  
//  * 
//  *  (c) 2010-2013 Ant Micro <antmicro.com>  
//  *  (c) 2011-2013 Realtime Embedded <rte.se>
//  *  All Rights Reserved.
//  * 
//  * NOTICE:  All information contained herein is, and remains    
//  * the property of Ant Micro and Realtime Embedded. 
//  * The intellectual and technical  concepts contained 
//  * herein are proprietary to Ant Micro and  are protected 
//  * by trade secret or copyright law.
//  * Dissemination of this information or reproduction of this material
//  * is strictly forbidden unless prior written permission is obtained
//  * from Ant Micro and Realtime Embedded.
//  *
//  */
using System;
using System.IO;

namespace AntShell.Encoding
{
    public static class EncodingHelper
    {
        public static char? ReadChar(Stream s, System.Text.Encoding e, bool withTimeout = false)
        {
            byte[] bytes = new byte[2];
            int res;

            for (int i = 0; i < 2; i++) {
                if (withTimeout && s.CanTimeout)
                {
                    s.ReadTimeout = 500;
                }
                res = s.ReadByte();

                if (res < 0)
                {
                    return null;
                }

                bytes[i] = (byte)res;
                var chars = e.GetChars(bytes, 0, i + 1)[0];
                if (!((CustomDecoderFallback)e.DecoderFallback).IsError) {
                    return chars;
                }
            }

            return null;
        }
    }
}

