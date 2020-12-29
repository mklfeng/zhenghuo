using Microsoft.Win32;
using NetFwTypeLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    class Program
    {
        RegistryKey firekey;
        //获取防火墙名称
        string firewallname = "";
        //电脑名称
        string versionname = "";
        //获取电脑版本名称
        public string getsysversion()
        {
            RegistryKey rk = Registry.LocalMachine.OpenSubKey(@"Software\\Microsoft\\Windows NT\\CurrentVersion");
            versionname = rk.GetValue("ProductName").ToString();
            rk.Close();
            return versionname;
        }
        //根据电脑类型来操作防火墙打开
        public void openfire(string versionname)
        {
            if (versionname.Contains("XP"))
            {
                firewallname = "SharedAccess";
                firekey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\\CurrentControlSet\\Services\\SharedAccess", true);
            }
            else
            {
                firewallname = "MpsSvc";
                firekey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\\CurrentControlSet\\Services\\MpsSvc", true);
            }
            //获取启动类型为禁止还是自动
            string start = firekey.GetValue("Start").ToString();
            if (start == "4")
            {
                ProcessStartInfo objProInfo = new ProcessStartInfo();
                objProInfo.FileName = "cmd.exe";
                objProInfo.CreateNoWindow = false;
                objProInfo.WindowStyle = ProcessWindowStyle.Hidden;
                objProInfo.Arguments = "/c sc config " + firewallname + " start= " + "auto";
                Process.Start(objProInfo);
                //挂起线程1s后启动服务
                System.Threading.Thread.Sleep(1000);
            }
            firekey.Close();
            //判断防火墙是否启动了
            ServiceController sc = new ServiceController(firewallname);
            //如果防火墙未启动则启动
            if (sc.Status.Equals(ServiceControllerStatus.Stopped) || sc.Status.Equals(ServiceControllerStatus.StopPending))
            {
                sc.Start();
            }
            //暂时不用
            if (versionname.Contains("XP"))
            {
                RegistryKey rekey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\\CurrentControlSet\\Services\\SharedAccess\\Parameters\\FirewallPolicy\\StandardProfile", true);
                var Enablefilewall = rekey.GetValue("EnableFirewall").ToString();
                if (Enablefilewall == "0")
                {
                    rekey.SetValue("EnableFirewall", 1);
                }
                rekey.Close();
            }
            else
            {
                INetFwPolicy2 firewallPolicy = (INetFwPolicy2)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));
                // 启用<高级安全Windows防火墙> - 专有配置文件的防火墙
                firewallPolicy.set_FirewallEnabled(NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PRIVATE, true);
                // 启用<高级安全Windows防火墙> - 公用配置文件的防火墙
                firewallPolicy.set_FirewallEnabled(NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PUBLIC, true);
            }
        }
        //为防火墙添加出站规则
        public void handle(string name)
        {
            //目前不用
            if (name.Contains("XP"))
            {
                INetFwAuthorizedApplication Fwapp = (INetFwAuthorizedApplication)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwAuthorizedApplication"));
            }

            else
            {
                // 1. 创建实例，阻止所有的出站连接
                INetFwPolicy2 firewallPolicy = (INetFwPolicy2)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));
                //启用或禁用<高级安全Windows防火墙> - 专有配置文件的出站连接
                firewallPolicy.set_DefaultOutboundAction(NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PRIVATE, NET_FW_ACTION_.NET_FW_ACTION_ALLOW);
                //启用或禁用<高级安全Windows防火墙> - 公用配置文件的出站连接
                firewallPolicy.set_DefaultOutboundAction(NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PUBLIC, NET_FW_ACTION_.NET_FW_ACTION_ALLOW);
                //创建出站规则来控制程序联网
                INetFwRule2 stopallRule = (INetFwRule2)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FWRule"));
                stopallRule.Name = "禁用所有端口号";
                stopallRule.Description = "关闭所有可用端口";
                stopallRule.Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_OUT;
                stopallRule.Action = NET_FW_ACTION_.NET_FW_ACTION_BLOCK;
                stopallRule.Protocol = (int)NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_TCP;
                stopallRule.Enabled = true;
                stopallRule.RemotePorts = "1-65535";
                firewallPolicy.Rules.Add(stopallRule);
                //添加成功,显示成功标志
                Console.WriteLine("关闭成功");
            }
        }
        //检测满足条件，开启所有访问
        public void AllowOpenFW()
        {
            //判断系统属于xp还是win7
            using (RegistryKey rk = Registry.LocalMachine.OpenSubKey(@"Software\\Microsoft\\Windows NT\\CurrentVersion"))
            {
                var VersionName = rk.GetValue("ProductName").ToString();
                if (VersionName.Contains("XP"))
                {
                    // 创建firewall管理类的实例 ，删除添加程序到防火墙例外
                    INetFwMgr netFwMgr = (INetFwMgr)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwMgr"));
                    netFwMgr.LocalPolicy.CurrentProfile.AuthorizedApplications.Remove("禁用所有端口号");
                }
                else
                {
                    // 1. 创建实例，允许所有程序的连接。
                    INetFwPolicy2 firewallPolicy = (INetFwPolicy2)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));
                    //启用或禁用<高级安全Windows防火墙> - 专有配置文件的出站连接
                    firewallPolicy.set_DefaultOutboundAction(NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PRIVATE, NET_FW_ACTION_.NET_FW_ACTION_ALLOW);
                    //启用或禁用<高级安全Windows防火墙> - 公用配置文件的出站连接
                    firewallPolicy.set_DefaultOutboundAction(NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PUBLIC, NET_FW_ACTION_.NET_FW_ACTION_ALLOW);
                    // 2. 删除本程序的出站规则删除规则
                    firewallPolicy.Rules.Remove("禁用所有端口号");
                    //添加成功,显示成功标志
                    Console.WriteLine("启动成功");
                }
            }
        }
        static void Main(string[] args)
        {
            start();
        }
       static void start()
        {
            Console.WriteLine("输入等待时间开始启动");
            string str = Console.ReadLine();
            Program p = new Program();
            string sysversion = p.getsysversion();
            p.openfire(sysversion);
            p.handle(sysversion);
            //挂起线程5s后启动服务
            System.Threading.Thread.Sleep(int.Parse(str + "000"));
            p.AllowOpenFW();
            start();
        }
    }
}