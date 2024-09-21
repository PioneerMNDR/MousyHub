using MousyHub.Components.Models.Services;

namespace MousyHub.Components.Models
{
    public class Person
    {
        public Person() 
        {
            if (Id == null)
            {
                Id = Guid.NewGuid().ToString();
            }
        }
        public Person(string name, string description,bool isUser,string key,string id = "",string? overrideSystemPromt=null)
        {
            Name = name;
            Description = description;
            IsUser = isUser;
            Avatar = UploaderService.LoadDefaultAvatar();
            OverrideSystemPromt = overrideSystemPromt;
            Key = key;
            if (id=="")
            {
                Id = Guid.NewGuid().ToString();
            }          
        }
        public Person(CharCard charCard, string key)
        {
            CharacterCard = charCard;
            Name=charCard.data.name; 
            Description=charCard.data.description;
            Avatar = charCard.avatarPNG;
            Key = key;
            Id = CharacterCard.system_name;
        }

   

        public string Id { get; set; }    
        public string Name { get; set; }
        public string Key { get; set; } 
        public string Description { get; set; }

        public byte[] Avatar { get; set; }

        public bool IsUser { get; set; } = false;

        public string? OverrideSystemPromt { get; set; }

        public CharCard? CharacterCard { get; set; }

    }
}
