﻿using MultiMiner.Utility.Forms;
using MultiMiner.Utility.OS;
using MultiMiner.Utility.Serialization;
using MultiMiner.Win.Data.Configuration;
using MultiMiner.Xgminer.Data;
using System;
using MultiMiner.Win.Extensions;
using System.Linq;
using MultiMiner.Engine;

namespace MultiMiner.Win.Forms.Configuration
{
    public partial class MinerSettingsForm : MessageBoxFontForm
    {
        private readonly MultiMiner.Engine.Data.Configuration.Xgminer minerConfiguration;
        private readonly MultiMiner.Engine.Data.Configuration.Xgminer workingMinerConfiguration;

        private readonly Application applicationConfiguration;
        private readonly Application workingApplicationConfiguration;

        private readonly Perks perksConfiguration;

        public MinerSettingsForm(MultiMiner.Engine.Data.Configuration.Xgminer minerConfiguration, Application applicationConfiguration,
            Perks perksConfiguration)
        {
            InitializeComponent();
            this.minerConfiguration = minerConfiguration;
            this.workingMinerConfiguration = ObjectCopier.CloneObject<MultiMiner.Engine.Data.Configuration.Xgminer, MultiMiner.Engine.Data.Configuration.Xgminer>(minerConfiguration);

            this.applicationConfiguration = applicationConfiguration;
            this.workingApplicationConfiguration = ObjectCopier.CloneObject<Application, Application>(applicationConfiguration);

            this.perksConfiguration = perksConfiguration;
        }

        private void AdvancedSettingsForm_Load(object sender, EventArgs e)
        {
            xgminerConfigurationBindingSource.DataSource = workingMinerConfiguration;
            applicationConfigurationBindingSource.DataSource = workingApplicationConfiguration;
            autoDesktopCheckBox.Enabled = OSVersionPlatform.GetGenericPlatform() != PlatformID.Unix;
            PopulateIntervalCombo();
            PopulateAlgorithmCombo();
            LoadSettings();

            algoArgCombo.Text = CoinAlgorithm.SHA256.ToString().ToSpaceDelimitedWords();
        }

        private void PopulateAlgorithmCombo()
        {
            algoArgCombo.Items.Clear();
            foreach (CoinAlgorithm algorithm in (CoinAlgorithm[])Enum.GetValues(typeof(CoinAlgorithm)))
            {
                if (AlgorithmIsSupported(algorithm))
                    algoArgCombo.Items.Add(algorithm.ToString().ToSpaceDelimitedWords());
            }
        }

        private static bool AlgorithmIsSupported(CoinAlgorithm algorithm)
        {
            return MinerFactory.Instance.DefaultMiners.ContainsKey(algorithm);
        }

        private void PopulateIntervalCombo()
        {
            intervalCombo.Items.Clear();
            foreach (Application.TimerInterval interval in (Application.TimerInterval[])Enum.GetValues(typeof(Application.TimerInterval)))
                intervalCombo.Items.Add(interval.ToString().ToSpaceDelimitedWords());
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            SaveSettings();
            ObjectCopier.CopyObject(workingMinerConfiguration, minerConfiguration);
            ObjectCopier.CopyObject(workingApplicationConfiguration, applicationConfiguration);
            DialogResult = System.Windows.Forms.DialogResult.OK;
        }

        private void LoadSettings()
        {
            intervalCombo.SelectedIndex = (int)workingApplicationConfiguration.ScheduledRestartMiningInterval;

            algoArgCombo.SelectedIndex = 0;
            
            autoDesktopCheckBox.Enabled = !workingMinerConfiguration.DisableGpu;

            LoadProxySettings();
        }

        private void LoadProxySettings()
        {
            MultiMiner.Engine.Data.Configuration.Xgminer.ProxyDescriptor proxy = minerConfiguration.StratumProxies.First();

            proxyPortEdit.Text = proxy.GetworkPort.ToString();
            stratumProxyPortEdit.Text = proxy.StratumPort.ToString();
        }

        private void SaveSettings()
        {
            //if the user has disabled Auto-Set Dynamic Intensity, disable Dynamic Intensity as well
            if (!workingApplicationConfiguration.AutoSetDesktopMode &&
                (workingApplicationConfiguration.AutoSetDesktopMode != applicationConfiguration.AutoSetDesktopMode))
                workingMinerConfiguration.DesktopMode = false;

            workingApplicationConfiguration.ScheduledRestartMiningInterval = (Application.TimerInterval)intervalCombo.SelectedIndex;

            SaveProxySettings();
        }

        private void SaveProxySettings()
        {
            MultiMiner.Engine.Data.Configuration.Xgminer.ProxyDescriptor proxy = minerConfiguration.StratumProxies.First();

            proxy.GetworkPort = int.Parse(proxyPortEdit.Text);
            proxy.StratumPort = int.Parse(stratumProxyPortEdit.Text);
        }

        private void argAlgoCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            CoinAlgorithm algorithm = (CoinAlgorithm)Enum.Parse(typeof(CoinAlgorithm), algoArgCombo.Text.Replace(" ", String.Empty));
            if (workingMinerConfiguration.AlgorithmFlags.ContainsKey(algorithm))
                algoArgEdit.Text = workingMinerConfiguration.AlgorithmFlags[algorithm];
            else
                algoArgEdit.Text = String.Empty;
        }

        private void algoArgEdit_Validated(object sender, EventArgs e)
        {
            CoinAlgorithm algorithm = (CoinAlgorithm)Enum.Parse(typeof(CoinAlgorithm), algoArgCombo.Text.Replace(" ", String.Empty));
            workingMinerConfiguration.AlgorithmFlags[algorithm] = algoArgEdit.Text;
        }

        private void advancedProxiesButton_Click(object sender, EventArgs e)
        {
            if (!perksConfiguration.PerksEnabled)
            {
                if (!perksConfiguration.PerksEnabled)
                {
                    System.Windows.Forms.MessageBox.Show(MiningEngine.AdvancedProxiesRequirePerksMessage,
                        "Perks Required", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
                }

                using (PerksForm perksForm = new PerksForm(perksConfiguration))
                {
                    perksForm.ShowDialog();
                }
            }
            else
            {
                using (ProxySettingsForm proxySettingsForm = new ProxySettingsForm(minerConfiguration))
                {
                    System.Windows.Forms.DialogResult dialogResult = proxySettingsForm.ShowDialog();
                    if (dialogResult == System.Windows.Forms.DialogResult.OK)
                    {
                        perksConfiguration.AdvancedProxying = true;
                        perksConfiguration.SavePerksConfiguration();

                        LoadProxySettings();
                    }
                }
            }
        }

        private void proxyPortEdit_KeyPress(object sender, System.Windows.Forms.KeyPressEventArgs e)
        {
            e.Handled = !char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar);
        }

        private void gpuSettingsLink_LinkClicked(object sender, System.Windows.Forms.LinkLabelLinkClickedEventArgs e)
        {
            using (GPUMinerSettingsForm gpuSettingsForm = new GPUMinerSettingsForm(workingMinerConfiguration))
            {
                System.Windows.Forms.DialogResult dialogResult = gpuSettingsForm.ShowDialog();
            }
        }
    }
}
