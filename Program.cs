using System;
using System.Net;
using System.Net.Sockets;
using SDL2;

namespace rcs;

class Program
{
    const int SERVER_PORT = 666;
    static bool useTcp = false;
    static TcpClient tcpClient;
    static NetworkStream tcpStream;
    static UdpClient udpClient;

    static byte MapKey(SDL.SDL_Keycode key)
    {
        return key switch
        {
            SDL.SDL_Keycode.SDLK_UP => (byte)'i',
            SDL.SDL_Keycode.SDLK_DOWN => (byte)'k',
            SDL.SDL_Keycode.SDLK_LEFT => (byte)'j',
            SDL.SDL_Keycode.SDLK_RIGHT => (byte)'l',
            SDL.SDL_Keycode.SDLK_w => (byte)'w',
            SDL.SDL_Keycode.SDLK_a => (byte)'a',
            SDL.SDL_Keycode.SDLK_s => (byte)'s',
            SDL.SDL_Keycode.SDLK_d => (byte)'d',
            SDL.SDL_Keycode.SDLK_SPACE => (byte)' ',
            SDL.SDL_Keycode.SDLK_ESCAPE => 27,
            SDL.SDL_Keycode.SDLK_RETURN => (byte)'\n',
            SDL.SDL_Keycode.SDLK_LCTRL => 17,
            SDL.SDL_Keycode.SDLK_RCTRL => 17,
            _ => 0
        };
    }

    static void SendPacket(byte[] packet)
    {
        try
        {
            if (useTcp)
            {
                tcpStream.Write(packet, 0, packet.Length);
            }
            else
            {
                udpClient.Send(packet, packet.Length);
            }
        }
        catch { /* ignore for now */ }
    }

    static void Main(string[] args)
    {
        if (args.Length != 2)
        {
            Console.WriteLine("Usage: program <udp|tcp> <server_ip>");
            return;
        }

        string protocol = args[0].ToLower();
        string serverIp = args[1];
        useTcp = protocol == "tcp";

        if (useTcp)
        {
            tcpClient = new TcpClient();
            tcpClient.Connect(serverIp, SERVER_PORT);
            tcpStream = tcpClient.GetStream();
            Console.WriteLine($"TCP connected to {serverIp}:{SERVER_PORT}");
        }
        else
        {
            udpClient = new UdpClient();
            udpClient.Connect(serverIp, SERVER_PORT);
            Console.WriteLine($"UDP ready to {serverIp}:{SERVER_PORT}");
        }

        if (SDL.SDL_Init(SDL.SDL_INIT_VIDEO) < 0)
        {
            Console.WriteLine($"SDL_Init failed: {SDL.SDL_GetError()}");
            return;
        }

        IntPtr window = SDL.SDL_CreateWindow(
            "Doom: Vector Edition: C# Edition (Input Window)",
            SDL.SDL_WINDOWPOS_CENTERED,
            SDL.SDL_WINDOWPOS_CENTERED,
            600, 550,
            SDL.SDL_WindowFlags.SDL_WINDOW_SHOWN
        );

        IntPtr renderer = SDL.SDL_CreateRenderer(window, -1, SDL.SDL_RendererFlags.SDL_RENDERER_ACCELERATED);

        bool running = true;
        SDL.SDL_Event e;

        while (running)
        {
            while (SDL.SDL_PollEvent(out e) != 0)
            {
                switch (e.type)
                {
                    case SDL.SDL_EventType.SDL_QUIT:
                        running = false;
                        break;

                    case SDL.SDL_EventType.SDL_KEYDOWN:
                    case SDL.SDL_EventType.SDL_KEYUP:
                        byte key = MapKey(e.key.keysym.sym);
                        if (key != 0)
                        {
                            byte pressed = e.type == SDL.SDL_EventType.SDL_KEYDOWN ? (byte)1 : (byte)0;
                            SendPacket(new byte[] { pressed, key });
                        }
                        break;
                }
            }

            SDL.SDL_SetRenderDrawColor(renderer, 40, 40, 40, 255);
            SDL.SDL_RenderClear(renderer);
            SDL.SDL_RenderPresent(renderer);
            SDL.SDL_Delay(1);
        }

        if (useTcp) tcpStream.Close();
        if (useTcp) tcpClient.Close();
        if (!useTcp) udpClient.Close();

        SDL.SDL_DestroyRenderer(renderer);
        SDL.SDL_DestroyWindow(window);
        SDL.SDL_Quit();
    }
}