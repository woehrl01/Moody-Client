namespace Moody
{
    public class Loc
    {
        public Loc(int id, string location)
        {
            this.Identiefier = id;
            this.Location = location;
        }

        public string Location
        {
            get; set;
        }

        public int Identiefier
        {
            get; set;
        }
    }
}