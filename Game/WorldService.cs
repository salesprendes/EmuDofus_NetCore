using Game.Command;
using Game.Database;
using Game.Database.Repository;
using Game.Frame;
using Game.Manager;
using Game.Network;
using Game.RPC;
using Protocolo.Framework.Command;
using Protocolo.Framework.Configuration;
using Protocolo.Framework.Configuration.Providers;
using Protocolo.Framework.Network;
using Protocolo.RPC.Protocol;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Game
{
    public class WorldService : AbstractTcpServer<WorldService, WorldClient>
    {
        public ConfigurationManager ConfigurationManager
        {
            get;
            private set;
        }

        public CommandManager<WorldCommandContext> CommandManager
        {
            get;
            private set;
        }

        public MessageDispatcher Dispatcher
        {
            get;
            private set;
        }

        public void Start(string archivo_configuracion)
        {
            ConfigurationManager = new ConfigurationManager();
            ConfigurationManager.RegisterAttributes();
            ConfigurationManager.Add(new JsonConfigurationProvider(archivo_configuracion), true);
            ConfigurationManager.Load();
            WorldClient.DebugEnabled = WorldConfig.LOG_DEBUG;

            CommandManager = new CommandManager<WorldCommandContext>();
            CommandManager.RegisterCommands();

            AddUpdatable(Dispatcher = new MessageDispatcher());
            AddUpdatable(RPCManager.Instance);

            AddTimer(WorldConfig.WORLD_SAVE_INTERVAL, SaveWorld);

            WorldDbMgr.Instance.Initialize();
            QuestManager.Instance.Initialize();
            InteractiveObjectManager.Instance.Initialize();
            JobManager.Instance.Initialize();
            ClientManager.Instance.Initialize();
            SpellManager.Instance.Initialize();
            AuctionHouseManager.Instance.Initialize();
            AreaManager.Instance.Initialize();
            NpcManager.Instance.Initialize();
            SpawnManager.Instance.Initialize();
            PaddockManager.Instance.Initialize();
            MapManager.Instance.Initialize();
            GuildManager.Instance.Initialize();
            EntityManager.Instance.Initialize();
            ConquestManager.Instance.Initialize();
            RPCManager.Instance.Initialize();

            string ip_game = GameServerRepository.Instance.GetById(WorldConfig.GAME_ID)?.Ip ?? "127.0.0.1";
            int puerto_game = GameServerRepository.Instance.GetById(WorldConfig.GAME_ID)?.Port ?? 5555;

            Start(ip_game, puerto_game);
        }

        #region Network

        protected override void OnClientConnected(WorldClient client)
        {
            AddMessage(() =>
            {
                client.FrameManager.AddFrame(AuthentificationFrame.Instance);
                client.Send(WorldMessage.HELLO_GAME());
            });
        }

        protected override void OnClientDisconnected(WorldClient client)
        {
            AddMessage(() =>
            {
                if (client.CurrentCharacter != null)
                {
                    EntityManager.Instance.CharacterDisconnected(client.CurrentCharacter);

                    client.Characters = null;
                    client.CurrentCharacter = null;
                }
                ClientManager.Instance.ClientDisconnected(client);
            });
        }

        protected override void OnDataReceived(WorldClient client, byte[] buffer, int offset, int count)
        {
            foreach (var message in client.Receive(buffer, offset, count))
            {
                if (WorldConfig.LOG_DEBUG)
                {
                    Logger.Debug("Client : " + message);
                }

                var character = client.CurrentCharacter;
                if (character != null)
                {
                    if (character.FrameManager != null)
                    {
                        if (!character.FrameManager.ProcessMessage(message))
                        {
                            character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                        }
                    }
                }
                else
                {
                    if (!client.FrameManager.ProcessMessage(message))
                    {
                        client.Send(WorldMessage.BASIC_NO_OPERATION());
                    }
                }
            }
        }

        #endregion

        public void SaveWorld()
        {
            var updateTimer = new Stopwatch();
            AddMessage(() =>
            {
                Dispatcher.Dispatch(WorldMessage.INFORMATION_MESSAGE(InformationTypeEnum.ERROR, InformationEnum.ERROR_WORLD_SAVING));
                RPCManager.Instance.UpdateState(GameStateEnum.STARTING);
                updateTimer.Start();

                Task.Run(() =>
                {
                    try
                    {
                        WorldDbMgr.Instance.UpdateAll();
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("WorldService::SaveWorld error : " + ex.Message);
                    }
                    finally
                    {
                        updateTimer.Stop();
                        AddMessage(() =>
                        {
                            Logger.Info($"WorldService : World update performed in : {updateTimer.ElapsedMilliseconds} ms");
                            RPCManager.Instance.UpdateState(GameStateEnum.ONLINE);
                            Dispatcher.Dispatch(WorldMessage.INFORMATION_MESSAGE(InformationTypeEnum.ERROR, InformationEnum.ERROR_WORLD_SAVING_FINISHED));
                        });
                    }
                });
            });
        }

        public void SaveWorldSync()
        {
            var timer = new Stopwatch();
            Logger.Info("WorldService : Guardando mundo antes de cerrar...");
            timer.Start();
            try
            {
                WorldDbMgr.Instance.UpdateAll();
            }
            catch (Exception ex)
            {
                Logger.Error($"WorldService::SaveWorldSync error: {ex}");
            }
            finally
            {
                timer.Stop();
                Logger.Info($"WorldService : Guardado completado en {timer.ElapsedMilliseconds} ms");
            }
        }

    }
}

