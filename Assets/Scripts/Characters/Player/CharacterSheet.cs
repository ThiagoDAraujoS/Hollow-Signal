using Utils;

namespace Characters.Player{
    public class CharacterSheet : Component{
        public Resource ap;
        public Resource mp;

        public bool HasAction => ap > 0;
        public bool HasMove   => mp > 0;
    }
}
