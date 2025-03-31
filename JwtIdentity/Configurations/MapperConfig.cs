namespace JwtIdentity.Configurations
{
    public class MapperConfig : AutomapperProfile
    {
        public MapperConfig()
        {
            //this.CreateMap<ApplicationUser, UserDto>().ReverseMap();

            _ = CreateMap<BaseModel, BaseViewModel>();
            _ = CreateMap<BaseViewModel, BaseModel>()
                .ForMember(x => x.CreatedBy, options => options.Ignore());

            _ = CreateMap<ApplicationUser, ApplicationUserViewModel>().ReverseMap();
            _ = CreateMap<ApplicationRole, ApplicationRoleViewModel>().ReverseMap();
            _ = CreateMap<RoleClaim, RoleClaimViewModel>().ReverseMap();

            _ = CreateMap<Question, QuestionViewModel>().IncludeAllDerived();
            _ = CreateMap<QuestionViewModel, Question>().IncludeAllDerived()
                .ForMember(x => x.CreatedBy, options => options.Ignore());

            _ = CreateMap<TextQuestion, TextQuestionViewModel>();
            _ = CreateMap<TextQuestionViewModel, TextQuestion>()
                .ForMember(x => x.CreatedBy, options => options.Ignore());

            _ = CreateMap<TrueFalseQuestion, TrueFalseQuestionViewModel>();
            _ = CreateMap<TrueFalseQuestionViewModel, TrueFalseQuestion>()
                .ForMember(x => x.CreatedBy, options => options.Ignore());

            _ = CreateMap<MultipleChoiceQuestion, MultipleChoiceQuestionViewModel>();
            _ = CreateMap<MultipleChoiceQuestionViewModel, MultipleChoiceQuestion>()
                .ForMember(x => x.CreatedBy, options => options.Ignore());

            _ = CreateMap<Rating1To10Question, Rating1To10QuestionViewModel>();
            _ = CreateMap<Rating1To10QuestionViewModel, Rating1To10Question>()
                .ForMember(x => x.CreatedBy, options => options.Ignore());


            _ = CreateMap<Answer, AnswerViewModel>().IncludeAllDerived();
            _ = CreateMap<AnswerViewModel, Answer>().IncludeAllDerived()
                .ForMember(x => x.CreatedBy, options => options.Ignore());

            _ = CreateMap<TextAnswer, TextAnswerViewModel>();
            _ = CreateMap<TextAnswerViewModel, TextAnswer>()
                .ForMember(x => x.CreatedBy, options => options.Ignore());

            _ = CreateMap<TrueFalseAnswer, TrueFalseAnswerViewModel>();
            _ = CreateMap<TrueFalseAnswerViewModel, TrueFalseAnswer>()
                .ForMember(x => x.CreatedBy, options => options.Ignore());

            _ = CreateMap<MultipleChoiceAnswer, MultipleChoiceAnswerViewModel>();
            _ = CreateMap<MultipleChoiceAnswerViewModel, MultipleChoiceAnswer>()
                .ForMember(x => x.CreatedBy, options => options.Ignore());

            _ = CreateMap<SingleChoiceAnswer, SingleChoiceAnswerViewModel>();
            _ = CreateMap<SingleChoiceAnswerViewModel, SingleChoiceAnswer>()
                .ForMember(x => x.CreatedById, options => options.Ignore());

            _ = CreateMap<Rating1To10Answer, Rating1To10AnswerViewModel>();
            _ = CreateMap<Rating1To10AnswerViewModel, Rating1To10Answer>()
                .ForMember(x => x.CreatedBy, options => options.Ignore());

            _ = CreateMap<ChoiceOption, ChoiceOptionViewModel>().ReverseMap();






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
