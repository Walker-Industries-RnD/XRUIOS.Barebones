using System;
using System.Collections.Generic;
using System.Text;
using static XRUIOS.Barebones.XRUIOS.VolumeClass;

namespace XRUIOS.Barebones
{
    public record SoundEQClass
    {
        public string EQName;
        public float Software;
        public float Effects;
        public float Voice;
        public float Music;
        public float Alerts;
        public float UI;
        public float Etc;
        public ExperimentalAudio OtherVol;

        public SoundEQClass() { }

        public SoundEQClass(string eqname, float software, float effects, float voice, float music, float alerts, float ui, float etc, ExperimentalAudio otherVol)
        {
            EQName = eqname;
            Software = software;
            Effects = effects;
            Voice = voice;
            Music = music;
            Alerts = alerts;
            UI = ui;
            Etc = etc;
            OtherVol = otherVol;

        }
    }

}
