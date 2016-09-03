using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Owin.Hosting;

namespace DynamicOdata.SelfHost
{
  public class Program
  {
    [STAThread]
    private static void Main(string[] args)
    {
      using (WebApp.Start<StartupOwin>("http://localhost:9000"))
      {
        Console.WriteLine("Press [enter] to quit...");
        Console.ReadLine();
      }
    }
  }
}