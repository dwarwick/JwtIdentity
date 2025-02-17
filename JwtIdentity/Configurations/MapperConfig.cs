namespace JwtIdentity.Configurations
{
    public class MapperConfig : AutomapperProfile
    {
        public MapperConfig()
        {
            //this.CreateMap<ApplicationUser, UserDto>().ReverseMap();

            _ = CreateMap<ApplicationUser, ApplicationUserViewModel>().ReverseMap();
            _ = CreateMap<ApplicationRole, ApplicationRoleViewModel>().ReverseMap();
            _ = CreateMap<RoleClaim, RoleClaimViewModel>().ReverseMap();


            _ = CreateMap<Answer, AnswerViewModel>();
            _ = CreateMap<AnswerViewModel, Answer>()
                .ForMember(x => x.CreatedBy, options => options.Ignore());


            _ = CreateMap<TextAnswer, TextAnswerViewModel>().ReverseMap();
            _ = CreateMap<TrueFalseAnswer, TrueFalseAnswerViewModel>().ReverseMap();
            _ = CreateMap<MultipleChoiceAnswer, MultipleChoiceAnswerViewModel>().ReverseMap();
            _ = CreateMap<SingleChoiceAnswer, SingleChoiceAnswerViewModel>().ReverseMap();

            _ = CreateMap<ChoiceOption, ChoiceOptionViewModel>().ReverseMap();

            _ = CreateMap<Question, QuestionViewModel>();
            _ = CreateMap<QuestionViewModel, Question>()
                .ForMember(x => x.CreatedBy, options => options.Ignore());

            _ = CreateMap<TextQuestion, TextQuestionViewModel>().ReverseMap();
            _ = CreateMap<TrueFalseQuestion, TrueFalseQuestionViewModel>().ReverseMap();
            _ = CreateMap<MultipleChoiceQuestion, MultipleChoiceQuestionViewModel>().ReverseMap();

            _ = CreateMap<Survey, SurveyViewModel>().ReverseMap();

            //this.CreateMap<BaseModel, BaseViewModel>().ReverseMap();

            //this.CreateMap<Forum, ForumViewModel>();

            //this.CreateMap<ForumViewModel, Forum>()
            //    .ForMember(q => q.CreatedBy, options => options.Ignore());

            //this.CreateMap<Post, PostViewModel>();
            //this.CreateMap<PostViewModel, Post>()
            //    .ForMember(x => x.CreatedBy, options => options.Ignore());

            //this.CreateMap<Tag, TagViewModel>();
            //this.CreateMap<TagViewModel, Tag>()
            //    .ForMember(x => x.Id, options => options.Ignore());

            //this.CreateMap<VoteViewModel, Vote>().ReverseMap();
        }
    }
}
