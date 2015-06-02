﻿using System;
using Inklewriter.Runtime;

namespace Inklewriter
{
	public class CommandLinePlayer
	{
		public Story story { get; protected set; }
		public bool autoPlay { get; set; }
        public Parsed.Story parsedStory { get; set; }

        public CommandLinePlayer (Story story, bool autoPlay = false, Parsed.Story parsedStory = null)
		{
			this.story = story;
			this.autoPlay = autoPlay;
            this.parsedStory = parsedStory;
		}

		public void Begin()
		{
			story.Begin ();

			Console.WriteLine(story.currentText);

			var rand = new Random ();

			while (story.currentChoices.Count > 0) {
				var choices = story.currentChoices;
				
                var choiceIdx = 0;
                bool choiceIsValid = false;
                Runtime.Path userDivertedPath = null;

				// autoPlay: Pick random choice
				if (autoPlay) {
					choiceIdx = rand.Next () % choices.Count;
				}

				// Normal: Ask user for choice number
				else {

					int i = 1;
					foreach (Choice choice in choices) {
						Console.WriteLine ("{0}: {1}", i, choice.choiceText);
						i++;
					}


                    do {
                        string userInput = Console.ReadLine ();

                        var inputParser = new InkParser (userInput);
                        object evaluatedInput = inputParser.CommandLineUserInput();

                        // Choice
                        if( evaluatedInput is int? ) {

                            choiceIdx = ((int)evaluatedInput) - 1;

                            if (choiceIdx < 0 || choiceIdx >= choices.Count) {
                                Console.WriteLine ("Choice out of range");
                            } else {
                                choiceIsValid = true;
                            }
                        }

                        // Help
                        else if( evaluatedInput is string && (string)evaluatedInput == "help" ) {
                            Console.WriteLine("Type a choice number, a divert (e.g. '==> myKnot'), an expression, or a variable assignment (e.g. 'x = 5')");
                        }
                            
                        // User entered some ink
                        else if( evaluatedInput is Parsed.Object ) {

                            // Variable assignment: create in Parsed.Story as well as the Runtime.Story
                            // so that we don't get an error message during reference resolution
                            if( evaluatedInput is Parsed.VariableAssignment ) {
                                var varAssign = (Parsed.VariableAssignment) evaluatedInput;
                                if( varAssign.isNewDeclaration ) {
                                    parsedStory.variableDeclarations[varAssign.variableName] = varAssign;
                                }
                            }

                            var parsedObj = (Parsed.Object) evaluatedInput;
                            parsedObj.parent = parsedStory;
                            parsedObj.GenerateRuntimeObject();
                            parsedObj.ResolveReferences(parsedStory);
                            var runtimeObj = parsedObj.runtimeObject;

                            // Divert
                            if( evaluatedInput is Parsed.Divert ) {
                                userDivertedPath = ((Parsed.Divert)evaluatedInput).runtimeDivert.targetPath;
                            }

                            // Expression or variable assignment
                            else if( evaluatedInput is Parsed.Expression || evaluatedInput is Parsed.VariableAssignment ) {
                                var result = story.EvaluateExpression((Container)runtimeObj);
                                if( result != null ) {
                                    Console.WriteLine(result);
                                }
                            }
                        }


                        else {
                            Console.WriteLine ("Unexpected input. Type 'help' or a choice number.");
                        }

                    } while(!choiceIsValid && userDivertedPath == null);

				}

                if (choiceIsValid) {
                    story.ContinueWithChoiceIndex (choiceIdx);
                } else if (userDivertedPath != null) {
                    story.ContinueFromPath (userDivertedPath);
                    userDivertedPath = null;
                }

				Console.WriteLine(story.currentText);
			}
		}
            
	}
}

