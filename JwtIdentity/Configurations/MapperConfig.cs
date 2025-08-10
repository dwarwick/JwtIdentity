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

            _ = CreateMap<ApplicationUser, ApplicationUserViewModel>()
                .ReverseMap()
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.SecurityStamp, opt => opt.Ignore())
                .ForMember(dest => dest.ConcurrencyStamp, opt => opt.Ignore());

            // Updated mapping for ApplicationRole and RoleClaim
            _ = CreateMap<ApplicationRole, ApplicationRoleViewModel>()
                .ForMember(dest => dest.Claims, opt => opt
                    .MapFrom(src => src.Claims));
                    
            _ = CreateMap<ApplicationRoleViewModel, ApplicationRole>();
            
            _ = CreateMap<RoleClaim, RoleClaimViewModel>().ReverseMap();

            // Updated mapping for Feedback
            _ = CreateMap<Feedback, FeedbackViewModel>();
            _ = CreateMap<FeedbackViewModel, Feedback>()
                .ForMember(x => x.CreatedBy, options => options.Ignore());

            _ = CreateMap<Question, QuestionViewModel>().IncludeAllDerived()
                .Include<MultipleChoiceQuestion, MultipleChoiceQuestionViewModel>();

            _ = CreateMap<QuestionViewModel, Question>().IncludeAllDerived()
                .ForMember(x => x.CreatedBy, options => options.Ignore());

            _ = CreateMap<TextQuestion, TextQuestionViewModel>();
            _ = CreateMap<TextQuestionViewModel, TextQuestion>()
                .ForMember(x => x.CreatedBy, options => options.Ignore());

            _ = CreateMap<TrueFalseQuestion, TrueFalseQuestionViewModel>();
            _ = CreateMap<TrueFalseQuestionViewModel, TrueFalseQuestion>()
                .ForMember(x => x.CreatedBy, options => options.Ignore());

            _ = CreateMap<MultipleChoiceQuestion, MultipleChoiceQuestionViewModel>()
                .ForMember(dest => dest.Options, opt => opt.MapFrom(src => src.Options));
            _ = CreateMap<MultipleChoiceQuestionViewModel, MultipleChoiceQuestion>()
                .ForMember(x => x.CreatedBy, options => options.Ignore());

            _ = CreateMap<Rating1To10Question, Rating1To10QuestionViewModel>();
            _ = CreateMap<Rating1To10QuestionViewModel, Rating1To10Question>()
                .ForMember(x => x.CreatedBy, options => options.Ignore());

            _ = CreateMap<SelectAllThatApplyQuestion, SelectAllThatApplyQuestionViewModel>()
                .ForMember(dest => dest.Options, opt => opt.MapFrom(src => src.Options));
            _ = CreateMap<SelectAllThatApplyQuestionViewModel, SelectAllThatApplyQuestion>()
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

            _ = CreateMap<SelectAllThatApplyAnswer, SelectAllThatApplyAnswerViewModel>();
            _ = CreateMap<SelectAllThatApplyAnswerViewModel, SelectAllThatApplyAnswer>()
                .ForMember(x => x.CreatedBy, options => options.Ignore());

            _ = CreateMap<ChoiceOption, ChoiceOptionViewModel>().ReverseMap()
                .ForMember(dest => dest.MultipleChoiceQuestionId, opt => opt.MapFrom(src => src.MultipleChoiceQuestionId))
                .ForMember(dest => dest.SelectAllThatApplyQuestionId, opt => opt.MapFrom(src => src.SelectAllThatApplyQuestionId));

            _ = CreateMap<Survey, SurveyViewModel>()
                .ForMember(dest => dest.NumberOfResponses, opt => opt.Ignore()); // Don't map this property from Survey model
            
            _ = CreateMap<SurveyViewModel, Survey>();
        }
    }
}
