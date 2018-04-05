﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LyNN
{
    public enum NodeType
    {
        input,
        node,
        output,
    }

    public class Node
    {
        public NodeType type;
        public float value;
        public List<Weight> parents;
        public List<Weight> children;
        public float bias;

        //error values
        public float error;
        public float bc;
        public int bc_count;
    }

    public class Weight
    {
        public float value;
        public Node parent;
        public Node child;

        public float vc;
        public int vc_count;
    }

    public class Network
    {
        public int numInputs;
        public int numOutputs;
        public int numLayers;

        private List<Node> inputs;
        private List<Node> outputs;
        private List<List<Node>> allNodes;
        private Random rand = new Random();


        /// <summary>
        /// Builds a network with the given amount of inputs, outputs, and hidden layers
        /// </summary>
        /// <param name="numInputs">Amount of inputs the network should have</param>
        /// <param name="layers">An array containing the amount of hidden nodes per layer</param>
        /// <param name="numOutputs">Amount of outputs the network should have</param>
        /// <returns>Returns a Network object</returns>
        public static Network buildNetwork(int numInputs, int[] layers, int numOutputs)
        {
            //Sanity checking arguments
            if (numInputs < 1) throw new Exception("Can't have less than one input");
            if (numOutputs < 1) throw new Exception("Can't have less than one output");

            //Create and initialize the network
            Network net = new Network();
            net.numInputs = numInputs;
            net.numOutputs = numOutputs;
            net.numLayers = layers.Length;
            net.inputs = new List<Node>();
            net.outputs = new List<Node>();
            net.allNodes = new List<List<Node>>();

            //Build the node structure
            List<Node> prevs = new List<Node>();
            for(int i = -1; i < layers.Length + 1; i++)
            {
                int nc;
                NodeType t;

                //Lazy method to build the network with inputs and outputs all as 'nodes'
                if (i == -1)
                {
                    nc = numInputs;
                    t = NodeType.input;
                }
                else if (i == layers.Length)
                {
                    nc = numOutputs;
                    t = NodeType.output;
                }
                else
                {
                    nc = layers[i];
                    t = NodeType.node;

                    //Sanity checking on amount of nodes in layer
                    if (nc < 1) throw new Exception("Can't have less than one node in a layer");
                }

                List<Node> news = new List<Node>();
                for(int j = 0; j < nc; j++)
                {
                    //Create a new node with the specified type
                    Node n = new Node();
                    n.type = t;
                    n.value = 0;
                    n.children = new List<Weight>();
                    n.parents = new List<Weight>();
                    n.bias = 0;

                    //Run through all of the nodes in the previous layers and connect them
                    if (prevs.Count > 0)
                    {
                        foreach (Node pn in prevs)
                        {
                            Weight w = new Weight();
                            w.child = n;
                            w.parent = pn;
                            w.value = 0;

                            pn.children.Add(w);
                            n.parents.Add(w);
                        }
                    }

                    //Add to the input/output lists
                    if (t == NodeType.input) net.inputs.Add(n);
                    if (t == NodeType.output) net.outputs.Add(n);

                    //Add to 'news' list just so that it can be used as a reference for the nodes in the next layer
                    news.Add(n);
                }

                //Add the new layer of nodes to the allNodes list
                net.allNodes.Add(news);

                prevs.Clear();
                prevs.AddRange(news);
            }

            return net;
        }

        /// <summary>
        /// Randomizes the weights and biases for the network
        /// </summary>
        public void randomizeNetwork()
        {
            for(int i = 0; i < allNodes.Count; i++)
            {
                for (int j = 0; j < allNodes[i].Count; j++)
                {
                    //Iterate through all nodes and randomize their bias
                    Node n = (allNodes[i])[j];
                    n.bias = (float)rand.NextDouble() - 0.5f;

                    //Then iterate through all of its children weights and randomize them
                    for (int k = 0; k < n.children.Count; k++)
                    {
                        Weight w = n.children[k];
                        w.value = (float)rand.NextDouble() - 0.5f;
                    }
                }
            }
        }

        /// <summary>
        /// Saves the network to file
        /// </summary>
        /// <param name="name">The filename to write it to</param>
        public void SaveNetwork(string name)
        {
            string nt = "";
            for(int i = 0; i < allNodes.Count; i++)
            {
                List<Node> nn = allNodes[i];

                //Write the amount of nodes in this layer on one line
                nt += nn.Count + "\n";
                foreach(Node n in nn)
                {
                    //Then, for each node, write the bias on one line, and all of its children weights in order of the node's index
                    nt += n.bias + "\n";
                    foreach (Weight w in n.children)
                    {
                        nt += w.value + ";";
                    }
                    nt += "\n";
                }
                nt += "\n";
            }
            File.WriteAllText(name, nt.Substring(0, nt.Length - 2)); //The -2 is to get rid of two newlines that would otherwise always exist at the end of the file
        }

        /// <summary>
        /// Loads a network from a file
        /// </summary>
        /// <param name="name">The filename to read from</param>
        public static Network LoadNetwork(string name)
        {
            //Create and initialize the network
            Network net = new Network();
            net.inputs = new List<Node>();
            net.outputs = new List<Node>();
            net.allNodes = new List<List<Node>>();

            //Read file
            //The layers are split by a double newline
            string[] all = File.ReadAllText(name).Split(new string[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
            net.numLayers = all.Length - 2;

            for(int i = 0; i < all.Length; i++)
            {
                net.allNodes.Add(new List<Node>());
            }

            for(int layer = all.Length - 1; layer >= 0; layer--)
            {
                string[] lines = all[layer].Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

                int num = int.Parse(lines[0]);

                //Add to numinputs/outputs vars
                if (layer == 0) net.numInputs = num;
                if (layer == all.Length - 1) net.numOutputs = num;

                for (int i = 0; i < num; i++)
                {
                    Node n = new Node();
                    n.children = new List<Weight>();
                    n.parents = new List<Weight>();

                    if (layer == 0)
                    {
                        //Input nodes get marked as input nodes and added to the list of input nodes
                        net.inputs.Add(n);
                        n.type = NodeType.input;
                    }
                    else if (layer == all.Length - 1)
                    {
                        //Output nodes get marked as output nodes and added to the list of output nodes
                        net.outputs.Add(n);
                        n.type = NodeType.output;
                    }
                    else n.type = NodeType.node; //Everything else is a regular node

                    //Add this node to the list of all nodes, then start reading the values out of the file
                    net.allNodes[layer].Add(n);
                    n.bias = float.Parse(lines[i * 2 + 1]);

                    if (i * 2 + 2 < lines.Length)
                    {
                        //Add all of the children weights in order
                        string[] ws = lines[i * 2 + 2].Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                        for(int j = 0; j < ws.Length; j++)
                        {
                            Weight w = new Weight();
                            w.parent = n;
                            w.child = net.allNodes[layer + 1][j];
                            w.value = float.Parse(ws[j]);
                            n.children.Add(w);
                            w.child.parents.Add(w);
                        }
                    }
                }
            }

            return net;
        }

        /// <summary>
        /// Sigmoid activation function
        /// </summary>
        /// <param name="val">The value to squish</param>
        /// <returns>Returns the squished (sigmoid) value of the given argument</returns>
        public static float sig(float val) { return (float)(1f / (1f + Math.Exp(-val))); }

        /// <summary>
        /// Evaluates what the network thinks about the inputs
        /// </summary>
        /// <param name="inputs">The inputs to evaluate</param>
        /// <returns>Returns the output of the network</returns>
        public float[] evaluate(float[] inputs)
        {
            if (inputs.Length != numInputs) throw new Exception("Incorrect number of inputs given! Got " + inputs.Length.ToString() + ", expected " + numInputs.ToString());

            for (int i = 0; i < inputs.Length; i++) this.inputs[i].value = inputs[i];

            //Calculate node value for each node in order
            for (int i = 0; i < allNodes.Count; i++)
            {
                for (int j = 0; j < allNodes[i].Count; j++)
                {
                    Node n = (allNodes[i])[j];
                    n.value = getNodeVal(n);
                }
            }

            //Read the output nodes and write them into a float array to return
            float[] ret = new float[numOutputs];
            for (int i = 0; i < outputs.Count; i++) ret[i] = outputs[i].value;

            return ret;
        }

        /// <summary>
        /// Takes in training data and sets the error values accordingly
        /// </summary>
        /// <param name="inputs">Training data inputs</param>
        /// <param name="goodOutputs">Training data outputs(that the network should strive to get right)</param>
        /// <returns>Returns the total error in the output, lower is better</returns>
        public float trainNetwork(float[] inputs, float[] goodOutputs)
        {
            //Feed the inputs to the network and evaluate
            float[] rets = evaluate(inputs);

            //Calculate the total error to return later
            float errorsum = 0;
            for (int i = 0; i < goodOutputs.Length; i++)
            {
                outputs[i].error = goodOutputs[i] - rets[i];
                errorsum += getError(goodOutputs[i], rets[i]);
            }

            //Go through all nodes backwards(from output to input) and adjust the weights based on the error values. Skip the output nodes
            for (int i = numLayers; i >= 0; i--)
            {
                List<Node> nodes = allNodes[i];
                for (int j = 0; j < nodes.Count; j++)
                {
                    backPropOne(nodes[j]);
                }
            }

            return errorsum;
        }

        /// <summary>
        /// Applies the average of all changes that the last training set proposed
        /// </summary>
        /// <param name="rate">The rate at which to change</param>
        public void applyTrainingChanges(float rate)
        {
            for (int i = numLayers; i >= 0; i--)
            {
                List<Node> nodes = allNodes[i];
                for (int j = 0; j < nodes.Count; j++)
                {
                    Node n = nodes[j];
                    for (int x = 0; x < n.children.Count; x++)
                    {
                        Weight cw = n.children[x];
                        cw.value += cw.vc / (float)cw.vc_count;
                        cw.vc = 0;
                        cw.vc_count = 0;
                    }
                    n.bias += n.bc / (float)n.bc_count;
                    n.bc = 0;
                    n.bc_count = 0;
                }
            }
        }

        /// <summary>
        /// Backpropagate this node based on its children's error values
        /// </summary>
        /// <param name="n">The node to calculate error values for</param>
        void backPropOne(Node n)
        {
            float sum = 0;
            for (int i = 0; i < n.children.Count; i++)
            {
                Weight cw = n.children[i];
                Node child = cw.child;
                float nact = child.value * (1 - child.value);

                //Adjust weight based on rate and add error to total error sum
                cw.vc += child.error * nact * n.value;
                cw.vc_count++;

                sum += child.error * nact * cw.value;
            }
            n.error = sum;

            n.bc += sum * n.value * (1 - n.value) * n.bias;
            n.bc_count++;
        }

        /// <summary>
        /// Gets the error value
        /// </summary>
        /// <param name="expected">The correct output value that the network should strive towards</param>
        /// <param name="actual">The actual output value of the network</param>
        /// <returns>Returns the error value</returns>
        float getError(float expected, float actual)
        {
            float diff = expected - actual;
            return 0.5f * diff * diff;
        }


        /// <summary>
        /// Gets the value attached to this weight(i.e. node value * weight)
        /// </summary>
        /// <param name="w">The given weight</param>
        /// <returns>Returns the value attached to this weight</returns>
        float mulWN(Weight w) { return w.value * w.parent.value; }

        /// <summary>
        /// Calculates the value that a node should be(without actually editing the value of the node)
        /// </summary>
        /// <param name="n">The node to calculate the value for</param>
        /// <returns>The value of the node</returns>
        float getNodeVal(Node n)
        {
            if (n.type == NodeType.input) return n.value;

            float sum = 0;
            float c = 0;
            foreach(Weight w in n.parents)
            {
                sum += mulWN(w);
                c++;
            }
            return sig(sum + n.bias);
        }
    }
}
